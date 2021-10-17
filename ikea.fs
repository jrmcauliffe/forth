compiletoflash

\res MCU: MSP430FR2433

\ TODO Ditch ones I'm not using
\ TIMERS
\res export TA0CTL TA0R TA0CCTL0 TA0CCTL1 TA0CCTL2 TA0CCR0 TA0CCR1 TA0CCR2 TA1CTL TA1CCTL0 TA1CCTL1 TA1CCTL2 TA1CCR0 TA1CCR1 TA1R
#include ms.fs
#include digital-io.fs


$0 variable buttonstate
$0 variable r1state
$0 variable r2state
50 variable lightLevel
\ Project pin assignments
1 1 io constant pLamp
1 2 io constant pLED
3 0 io constant pRotary1
3 1 io constant pRotary2
3 2 io constant pButton
1 constant step



\ Debouncing constants
250 constant ticks_per_ms
8 constant debounce_ms
debounce_ms 1000 ticks_per_ms / / constant debounce_ticks
$FFFF debounce_ticks 1 - lshift constant debounce_check
debounce_check shl constant debounce_mask

\ Fix value between max and min values
: clamp 90 min 0 max ;
\ Simple squared gamma 0-90
: light dup * TA0CCR1 ! lightLevel @ . cr ;
    
: tick-interrupt-handler
  buttonstate @ shl pButton io@ or debounce_mask or dup buttonstate ! debounce_check = if 45 dup lightLevel ! light  then
  r1state @ shl pRotary1 io@ or debounce_mask or dup r1state !
  r2state @ shl pRotary2 io@ or debounce_mask or dup r2state !
  2dup
  debounce_check = swap debounce_mask = and
  -rot swap
  debounce_check = swap debounce_mask = and

  \ Turn Right
  if lightLevel @ step + clamp dup lightLevel ! light then
  \ Turn Left
  if lightLevel @ step - clamp dup lightLevel ! light then

\ pButton buttonState triggered? if pLED iox! then
;

: myinit \ ( -- )
  \ Configure pins for io
  OUTMODE-SP1 pLED     io-mode! \ Indicator LED
  OUTMODE-SP1 pLamp    io-mode! \ Lamp
  INMODE-PU   pButton  io-mode! \ Rotary Pushbutton
  INMODE-PU   pRotary1 io-mode! \ Rotary Quadrature 1
  INMODE-PU   pRotary2 io-mode! \ Rotary Quadrature 2

  \ Timer A0 for running Lamp / led dimming duty
  $0008             TA0CTL bis! \ Set TACLR to clear timer
  lightLevel dup *  TA0CCR1 !    \ Lamp initial duty cycle
  $07FF             TA0CCR2 !    \ LED initial duty cycle
  $1FFF             TA0CCR0 !    \ Frequency
  $00E0             TA0CCTL1 !
  $00E0             TA0CCTL2 !
  $210              TA0CTL !     \ SMCLK/1 Start in up mode

  \ Timer A1 for switch debounce and clock time
  $2D0 TA1CTL !   \ SMCLK/8 - Up Mode - disable interrupts
  1000 ticks_per_ms / 1000 * TA1CCR0 ! \ 25Hz
  $10  TA1CCTL0 ! \ Enable interupts

  \ Register interrupt handlers  
  ['] tick-interrupt-handler irq-timerb0 ! \ (B0 is mecrisp's confusing name for A1 main interrupt)
  \ Enable interrupts
  eint
;


: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else myinit then
; 

compiletoram
