\ Switch: P2.0 (input, internal pull-up, active-low)

cr here u.   \ dict-start

\ ----- GPIO registers -----
$201 constant P2IN     \ port 2 input
$203 constant P2OUT    \ port 2 output / pull-direction
$207 constant P2REN    \ port 2 resistor enable

1 constant SW_BIT      \ P2.0 mask

: io-init ( -- )
  \ Defaults to input, enable pullup.
  SW_BIT P2OUT cbis!     \ pull-up direction
  SW_BIT P2REN cbis!     \ enable pull resistor
;

io-init

\ ===== Ganssle 16-bit shift-register debouncer =====

$0 variable shift-state     \ 16-bit history; high 3 bits saturate to 1
0  variable press-count     \ debounced presses since last shift-start

\ ----- Helpers -----
: switch? ( -- flag )  SW_BIT P2IN cbit@ ; inline   \ -1 released, 0 pressed

: shift-isr ( -- )
  shift-state @ 2*                       \ shift window left (16-bit truncates)
  switch? 1 and or                       \ append sample (1=released, 0=pressed)
  $E000 or  shift-state !                \ saturate top 3 bits, store
  shift-state @ $F000 = if
    1 press-count +!
    cr press-count @ .
  then
;

\ ----- Timer A2 registers -----
$400 constant TA2CTL       \ Timer A2 control
$402 constant TA2CCTL0     \ Capture/compare control 0
$410 constant TA2R         \ Timer A2 free-running counter
$412 constant TA2CCR0      \ Capture/compare register 0

: shift-start ( -- )
  0 shift-state !  0 press-count !       \ zero debounce state and count
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
