\ Raw P2.0 GPIO interrupt — no debouncing.
\ Each falling edge fires the ISR, so a single physical press produces
\ multiple counts as the contacts bounce. This is the "why you need a
\ debouncer" demo. Pair with db-timer.fs for the filtered counterpart.
\
\ Switch: P2.0 (input, internal pull-up, active-low)
\ LED:    P3.0 (output, starts low) — used by mirror-loop sanity check

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

\ ----- Port 2 interrupt registers -----
$219 constant P2IES        \ port 2 interrupt edge select
$21B constant P2IE         \ port 2 interrupt enable
$21D constant P2IFG        \ port 2 interrupt flag

\ ===== Raw GPIO interrupt (no debounce) =====
0 variable raw-count       \ ISR firings since last raw-start

: raw-isr ( -- )
  SW_BIT P2IFG cbic!                     \ clear flag (else re-fires forever)
  1 raw-count +!
  cr raw-count @ .
;

: raw-start ( -- )
  0 raw-count !
  ['] raw-isr irq-port2 !                \ install handler
  SW_BIT P2IES cbis!                     \ falling edge (high→low = press)
  SW_BIT P2IFG cbic!                     \ clear any pending flag
  SW_BIT P2IE  cbis!                     \ enable interrupt for P2.0
  eint                                   \ global interrupt enable
;

: raw-stop ( -- )
  SW_BIT P2IE cbic!                      \ mask P2.0 interrupt
;
