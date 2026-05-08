\ Timer-debounced switch input on P2.0.
\ Each tick (5 ms) shifts a 4-bit history register and OR's in the
\ inverted sample; hist=$F means "pressed last 4 ticks straight",
\ hist=0 means "released last 4 ticks straight". A debounced press
\ bumps press-count and prints the new value on a fresh line.
\ Pair with db-gpio.fs to compare against a no-debounce baseline.
\
\ Switch: P2.0 (input, internal pull-up, active-low)
\ LED:    P3.0 (output, starts low) — used by mirror-loop sanity check

cr here u.   \ dict-start

\ ----- MSP430FR2355 port registers we touch -----
\ Layout: PBASE=$200, pair stride=$20, odd port=low byte, even port=high byte.
\ Port-byte offsets: IN=$0 OUT=$2 DIR=$4 REN=$6 ...
$201 constant P2IN     \ port 2 input
$203 constant P2OUT    \ port 2 output / pull-direction
$207 constant P2REN    \ port 2 resistor enable
$222 constant P3OUT    \ port 3 output
$224 constant P3DIR    \ port 3 direction

1 constant SW_BIT      \ P2.0 mask
1 constant LED_BIT     \ P3.0 mask

: io-init ( -- )
  \ ---- Switch P2.0 ----  input + internal pull-up
  \ DIR is 0 after reset (input); leave it.
  \ OUT BEFORE REN: with REN=1 the OUT bit selects pull-up (1) vs
  \ pull-down (0); flipping REN on while OUT was still 0 would briefly
  \ pull the line low and give a spurious "pressed" reading.
  SW_BIT P2OUT cbis!     \ pull-up direction
  SW_BIT P2REN cbis!     \ enable pull resistor

  \ ---- LED P3.0 ----  output, off
  \ OUT BEFORE DIR: ensure OUT=0 before making the pin a driver, so the
  \ LED comes up off rather than reflecting whatever stale OUT bit was.
  LED_BIT P3OUT cbic!    \ LED off
  LED_BIT P3DIR cbis!    \ drive the pin
;

io-init

\ ----- Helpers -----
: switch? ( -- flag )  SW_BIT P2IN cbit@ ;          \ -1 released, 0 pressed
: led-on     ( -- )    LED_BIT P3OUT cbis! ;
: led-off    ( -- )    LED_BIT P3OUT cbic! ;
: led!       ( ? -- )  if led-on else led-off then ;
: led-toggle ( -- )    LED_BIT P3OUT cxor! ;

\ ----- Live sanity check -----
\ Switch is active-low, so invert before driving the LED.
: mirror      ( -- )   switch? 0= led! ;

\ Exits on any keypress at the terminal. key? checks without consuming;
\ key drop swallows the trigger so it doesn't get parsed as Forth on exit.
: mirror-loop ( -- )   begin mirror key? until  key drop ;

\ ----- Timer A2 registers -----
$400 constant TA2CTL       \ Timer A2 control
$402 constant TA2CCTL0     \ Capture/compare control 0
$410 constant TA2R         \ Timer A2 free-running counter
$412 constant TA2CCR0      \ Capture/compare register 0

\ ===== Forth shift-register debouncer (Ganssle) =====
\ At 200 Hz tick (5 ms) that's a 20 ms settle window — squarely in
\ Ganssle's recommended range for tactile switches.

$0 variable shift-hist
$0 variable shift-on        \ 1 = currently pressed (debounced)
0  variable press-count     \ debounced presses since last shift-start

: shift-isr ( -- )
  shift-hist @ 1 lshift                  \ shift window left
  switch? 0= 1 and or                    \ append inverted sample (1=pressed)
  $F and shift-hist !                    \ keep low 4 bits
  shift-hist @ $F = shift-on @ 0= and
    if  1 press-count +!
        cr press-count @ .
        1 shift-on !  then               \ rising edge: count + print
  shift-hist @ 0= shift-on @ and
    if  0 shift-on !  then               \ falling edge: re-arm
;

: shift-start ( -- )
  0 shift-hist !  0 shift-on !  0 press-count !
  ['] shift-isr irq-timerc0 !            \ install handler
  $290  TA2CTL   !                       \ SMCLK + /4 + up mode
  20000 TA2CCR0  !                       \ 4 MHz / 200 Hz = 5 ms tick
  $10   TA2CCTL0 !                       \ enable CCIE
  eint                                   \ set GIE so the ISR actually fires
;

: shift-stop ( -- )
  0 TA2CCTL0 !                           \ disable CCIE
  0 TA2CTL   !                           \ stop timer
;

cr here u.   \ dict-end

#include bench.fs
