\ Ganssle 16-bit shift-register debouncer on P2.0 — same algorithm as
\ db-ganssle.fs, but the timer ISR and the hardware-init code are
\ written in MSP430 assembly via the e4thcom xas plug-in. Sibling of
\ db-asm.fs (which is the asm twin of db-timer.fs).
\
\ Algorithm (per Ganssle):
\   State = (State << 1) | !RawKeyPressed() | $E000;
\   if (State == $F000) press!
\
\ Sample sense: 1 = released, 0 = pressed (matches Ganssle's
\ !RawKeyPressed(), opposite of db-asm.fs / db-timer.fs).
\
\ Prereqs: lib/xas-code.fs must be #include-ed first (it defines the
\ `code` / `end-code` glue used by the xas plug-in).

cr here u.   \ dict-start

#include lib/xas-code.fs

\xas MCU: MSP430FR2355
\xas 1 equ SW_BIT                  \ P2.0 mask, asm-only

\ ----- State and Forth-side helpers -----
\ shift-state lives in R08 across ISR invocations — Mecrisp documents
\ R08/R09 as unused, so they're free for persistent user state.
0 variable press-count     \ debounced presses since last shift-start

: print-press  ( -- )      cr press-count @ . ;    \ called from the asm ISR

\ ===== io-init in assembler =====
code io-init
  SW_BIT # P2OUT & .b bis            \ pull-up direction (OUT before REN)
  SW_BIT # P2REN & .b bis            \ enable pull resistor
  next
end-code

io-init

\ ===== ISR in assembler =====
\ Each tick (R08 holds shift-state across invocations):
\   R08 <<= 1                          (16-bit truncates the top bit)
\   if (P2.0 high)  R08 |= 1           (sample = 1 means released)
\   R08 |= $E000                       (saturate top 3 bits)
\   if (R08 == $F000) press-count++; print-press
code shift-isr
  R08 R08 add                          \ shift left 1 (= ADD R08,R08)
  SW_BIT # P2IN & .b bit               \ Z=1 if line low (pressed)
  0<>? if,  #1 R08 bis  then,          \ if released, OR 1 into low bit
  $E000 # R08 bis                      \ saturate top 3 bits

  $F000 # R08 cmp
  =? if,
    #1 x[ press-count ] & add          \ press-count++
    x[ ' print-press ] # call          \ live print
  then,
  next
end-code

\ ===== Start / stop in assembler =====
code shift-start
  #0 R08 mov                                  \ init register-resident state
  #0 x[ press-count ] & mov                   \ zero counter
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
