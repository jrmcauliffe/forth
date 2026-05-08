\ Timer-debounced switch input on P2.0 — same shift-register algorithm as
\ db-timer.fs, but the timer ISR and the hardware-init code are written in
\ MSP430 assembly via the e4thcom xas plug-in. This is the variant that
\ shows when assembler is worth the trouble: the ISR fires every 5 ms and
\ wants to be tight, and the init code talks to peripheral registers by
\ name with no Forth constants of our own to maintain.
\
\ Prereqs: lib/xas-code.fs must be #include-ed first (it defines the
\ `code` / `end-code` glue used by the xas plug-in).

cr here u.   \ dict-start

#include lib/xas-code.fs

\xas MCU: MSP430FR2355
\ \res export P2IN P2OUT P2REN TA2CTL TA2CCTL0 TA2CCR0
\ \res 1 equ SW_BIT
\ \res export SW_BIT

\ ----- State and Forth-side helpers -----
0 variable shift-hist
0 variable shift-on        \ 1 = currently pressed (debounced)
0 variable press-count     \ debounced presses since last shift-start

\ : switch?      ( -- flag )  SW_BIT P2IN cbit@ ;     \ -1 released, 0 pressed
: print-press  ( -- )       cr press-count @ . ;    \ called from the asm ISR

\ ===== io-init in assembler =====
code io-init
  SW_BIT # P2OUT & .b bis            \ pull-up direction (OUT before REN)
  SW_BIT # P2REN & .b bis            \ enable pull resistor
  next
end-code

io-init

\ ===== ISR in assembler =====
\ Each tick: shift `shift-hist` left, OR in the inverted (active-high)
\ sample, mask to a 4-bit window. hist=$F means "pressed last 4 ticks
\ straight"; hist=0 means "released last 4 ticks straight". On the
\ rising debounced edge, bump press-count and call print-press.
\
\ R7 (alias `w`) is Mecrisp's IRQ-scratch register and is saved on
\ interrupt entry, so we use it freely.
code shift-isr
  x[ shift-hist ] & w mov              \ w = shift-hist @
  w w add                              \ shift left 1 (= ADD w,w)
  SW_BIT # P2IN & .b bit               \ Z=1 if line low (pressed)
  0=? if,  #1 w bis  then,             \ if pressed, OR 1 into low bit
  $F # w and>                          \ keep low 4 bits
  w x[ shift-hist ] & mov              \ store back

  \ Rising edge: hist == $F && shift-on == 0
  $F # w cmp
  =? if,
    #0 x[ shift-on ] & cmp
    =? if,
      #1 x[ press-count ] & add        \ press-count++
      #1 x[ shift-on ] & mov           \ mark as pressed
      x[ ' print-press ] # call        \ live print
    then,
  then,

  \ Release: hist == 0 && shift-on != 0
  #0 w cmp
  =? if,
    #0 x[ shift-on ] & cmp
    <>? if,  #0 x[ shift-on ] & mov  then,
  then,
  next
end-code

\ ===== Start / stop in assembler =====
code shift-start
  #0 x[ shift-hist ] & mov
  #0 x[ shift-on ] & mov
  #0 x[ press-count ] & mov
  x[ ' shift-isr ] # x[ irq-timerc0 ] & mov   \ install handler
  $290  # TA2CTL & mov                        \ SMCLK + /4 + up mode
  $4E20 # TA2CCR0 & mov                       \ 4 MHz / 200 Hz = 5 ms tick (=20000)
  $10 # TA2CCTL0 & mov                        \ enable CCIE
  eint                                        \ set GIE
  next
end-code

code shift-stop
  #0 TA2CCTL0 & mov                  \ disable CCIE
  #0 TA2CTL & mov                    \ stop timer
  next
end-code

cr here u.   \ dict-end

#include bench.fs
