compiletoflash

\res MCU: MSP430FR2433

\ TIMERS
\res export TA0CTL TA0CCTL1 TA0CCTL2 TA0CCR0 TA0CCR1 TA0CCR2 TA1CTL TA1CCTL0 TA1CCR0
#include ms.fs
#include digital-io.fs

\ Debounce status tracking variables & constants
\ see http://www.ganssle.com/debouncing.htm for details
\ Real work is done in the tick-interrupt-handler
$0 variable buttonstate                                    \ Encoder bounce tracker
$0 variable r1state                                        \ Encoder rotation 1 tracker
$0 variable r2state                                        \ Encoder rotation 2 tracker
$0 variable ticks                                          \ Keep track of the passage of ticks
250 constant ticks_per_sec                                 \ Number of ticks in a second
8 constant debounce_ms                                     \ Settle time for switch debounce
debounce_ms 1000 ticks_per_sec / / constant debounce_ticks \ Settle time in ticks
$FFFF debounce_ticks 1 - lshift    constant debounce_check \ Constant for tracking debounce
debounce_check shl                 constant debounce_mask  \ Constant for tracking debounce

\ State Variables
50 variable origLightLevel            \ The user set value for light
50 variable lightLevel                \ The system 'dimmed' value for light
30 variable timeoutSeconds            \ User selectable timeout
timeoutSeconds @ variable secondsLeft \ How long until we shut it down
0  variable ticks                     \ Track ticks to track seconds
\ MCU  pin assignments
1 1 io constant pLamp
1 2 io constant pLED
3 0 io constant pRotary1
3 1 io constant pRotary2
3 2 io constant pButton

\ Fix value between max and min values
: clamp 90 min 0 max ;

\ Simple squared gamma 0-90
: light dup * TA0CCR1 ! lightLevel @ . cr ;

\ TODO Can we use a slower clock to track ticks?
\ TODO Setup LED nightlight timeout
\ TODO User setup mode to set timeout / mode
\ TODO Can we get more clever with p3 debounce check?
: tick-interrupt-handler
  ticks @ 1+ ticks_per_sec mod
  dup ticks !              \ inc ticks rollover
  0= if                    \ Have we crossed a sec?
    secondsLeft @ 1-       \ Decrement timeout secs
    dup secondsLeft !
    0= if                  \ Down to zero?
      0 lightLevel !       \ Lights out
    then
  then


  \ If button clicked, return light to known value
  buttonstate @ shl pButton io@ or debounce_mask or dup buttonstate !
  debounce_check = if
    origLightLevel @ lightLevel !  \ Back to default light level
    timeoutSeconds @ secondsLeft ! \ Reset timers
    0 ticks !
  then

  \ Record state of encoder switches
  r1state @ shl pRotary1 io@ or debounce_mask or dup r1state !
  r2state @ shl pRotary2 io@ or debounce_mask or dup r2state !
  2dup
  debounce_check = swap debounce_mask = and
  -rot swap
  debounce_check = swap debounce_mask = and

  \ Are we turning left or right? Update the desired light level
  \ Turn Right
  if lightLevel @ 2+ clamp lightLevel ! then
  \ Turn Left
  if lightLevel @ 2- clamp lightLevel ! then

  \ 'Close in' on desired value to avoid abrupt light level changes
  \ But scale the shifts so that big jumps don't take forever (pressing the button etc)

  lightLevel @ dup * TA0CCR1 @ \ Calculate the desired TA0CCR1 by squaring desired level
  dup -rot - 5 arshift         \ Find the difference and then divide this by 2^5 (16)
  dup 0= if drop               \ if 0 then we're close enough to assume the desired value
  else + then TA0CCR1 !        \ otherwise add offset to close in on desired TA0CCR value
;

: myinit \ ( -- )
  \ Configure pins for io
  OUTMODE-SP1 pLED     io-mode! \ Indicator LED
  OUTMODE-SP1 pLamp    io-mode! \ Lamp
  INMODE-PU   pButton  io-mode! \ Rotary Pushbutton
  INMODE-PU   pRotary1 io-mode! \ Rotary Quadrature Enc 1
  INMODE-PU   pRotary2 io-mode! \ Rotary Quadrature Enc 2

  \ Timer A0 for running PWM Lamp / LED dimming duty
  $0008             TA0CTL bis! \ Set TACLR to clear timer
  $1FFF             TA0CCR0 !    \ Frequency
  $0000             TA0CCR1 !    \ Lamp initial duty cycle (tick will move this to lightLevel)
  $07FF             TA0CCR2 !    \ LED initial duty cycle
  $00E0             TA0CCTL1 !
  $00E0             TA0CCTL2 !
  $210              TA0CTL !     \ SMCLK/1 Start in up mode

  \ Timer A1 for switch debounce and clock time
  $2D0 TA1CTL !   i                     \ SMCLK/8 - Up Mode
  1000 ticks_per_sec / 1000 * TA1CCR0 ! \ trigger ever ticks_per_sec
  $10  TA1CCTL0 !                       \ Enable interupts

  \ Register interrupt handlers and enable interrupts
  ['] tick-interrupt-handler irq-timerb0 ! \ (B0 is mecrisp's confusing name for A1 main interrupt)
  eint
;


: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else myinit then
; 

compiletoram
