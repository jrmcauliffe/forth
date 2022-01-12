compiletoflash

\res MCU: MSP430FR2433

\ TIMERS
\res export TA0CTL TA0CCTL1 TA0CCTL2 TA0CCR0 TA0CCR1 TA0CCR2 TA2CTL TA2CCTL0 TA2CCR0 TA3CTL TA3CCTL0 TA3CCR0
\res export RTCCTL RTCIV RTCMOD RTCCNT
#include ms.fs
#include digital-io.fs

\ Debounce status tracking variables & constants
\ see http://www.ganssle.com/debouncing.htm for details
\ Real work is done in the tick-interrupt-handler
$0 variable buttonstate                                    \ Encoder bounce tracker
$0 variable r1state                                        \ Encoder rotation 1 tracker
$0 variable r2state                                        \ Encoder rotation 2 tracker
250 constant d_ticks_per_sec                               \ Number of debounce ticks in a second
64 constant ticks_per_sec                                  \ Number of clock ticks in a second
8 constant debounce_ms                                     \ Settle time for switch debounce
debounce_ms 1000 d_ticks_per_sec / / constant debounce_ticks \ Settle time in ticks
$FFFF debounce_ticks 1 - lshift    constant debounce_check \ Constant for tracking debounce
debounce_check shl                 constant debounce_mask  \ Constant for tracking debounce

\ State Variables
\ Swoles
20 constant origLightLevel
600 constant timeoutSeconds

\ Bear
\ 50 constant origLightLevel
\ 1800 constant timeoutSeconds

origLightLevel variable lightLevel                \ The system 'dimmed' value for light

\ MCU  pin assignments
1 1 io constant pLED
1 2 io constant pLamp
3 1 io constant pRotary1
3 0 io constant pRotary2
3 2 io constant pButton

\ Fix value between max and min values
: clamp 90 min 0 max ;

\ TODO Can we get more clever with p3 debounce check?
: debounce-tick-interrupt-handler
  \ If button clicked, return light to known value
  buttonstate @ shl pButton io@ or debounce_mask or dup buttonstate !
  debounce_check = if
    lightLevel @ 0= if               \ Light is off, revert back to original
      origLightLevel lightLevel !    \ Back to default light level
    else
      0 lightLevel !                 \ Otherwise button press is off
    then
    $0042 RTCCTL bis!                \ Reset countdown timer and enable interrupts
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
;

: clock-tick-interrupt-handler
  \ 'Close in' on desired value to avoid abrupt light level changes
  \ But scale the shifts so that big jumps don't take forever (pressing the button etc)
  lightLevel @ dup * TA0CCR2 @ \ Calculate the desired TA0CCR2 by squaring desired level
  dup -rot - 3 arshift         \ Find the difference and then divide this by 2^3 (8)
  dup 0= if drop               \ if 0 then we're close enough to assume the desired value
  else + then TA0CCR2 !        \ otherwise add offset to close in on desired TA0CCR value
;

: rtc-interrupt-handler
  0 lightLevel ! \ Lights out
  RTCIV @ drop   \ Clear interrupt
  2 RTCCTL bic!  \ Disable interrupt
;

: myinit \ ( -- )
  \ Configure pins for io
  OUTMODE-SP1 pLED     io-mode! \ Indicator LED
  OUTMODE-SP1 pLamp    io-mode! \ Lamp
  INMODE-PU   pButton  io-mode! \ Rotary Pushbutton
  INMODE-PU   pRotary1 io-mode! \ Rotary Quadrature Enc 1
  INMODE-PU   pRotary2 io-mode! \ Rotary Quadrature Enc 2

  \ Timer A0 for running PWM Lamp / LED dimming duty
  $0008             TA0CTL bis!  \ Set TACLR to clear timer
  $1FFF             TA0CCR0 !    \ Frequency
  $07FF             TA0CCR1 !    \ LED initial duty cycle
  $0000             TA0CCR2 !    \ Lamp initial duty cycle (tick will move this to lightLevel)
  $00E0             TA0CCTL1 !
  $00E0             TA0CCTL2 !
  $210              TA0CTL !     \ SMCLK/1 Start in up mode

  \ Timer A2 for switch debounce
  $2D0 TA2CTL !                           \ SMCLK/8 - Up Mode
  1000 d_ticks_per_sec / 1000 * TA2CCR0 ! \ trigger ever ticks_per_sec
  $10  TA2CCTL0 !                         \ Enable interupts

  \ Timer A3 for updating lamp values
  $110 TA3CTL !                         \ ACLK/1 - Up Mode
  $7FFF ticks_per_sec /  TA3CCR0 !
  $10  TA3CCTL0 !                       \ Enable interupts

  \ RTC for timeout
  $3302   RTCCTL !                      \ VCLOCK / 1000 /w interrupts
  timeoutSeconds 10 * RTCMOD !          \ 10 ticks per second
  $0040   RTCCTL bis!

  \ Register interrupt handlers and enable interrupts
  ['] debounce-tick-interrupt-handler irq-timerc0 ! \ (C0 is mecrisp's confusing name for A2 main interrupt)
  ['] clock-tick-interrupt-handler irq-timerd0 !    \ (D0 is mecrisp's confusing name for A3 main interrupt)
  ['] rtc-interrupt-handler irq-rtc !               \ RTC handler

  eint
;


: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else myinit then
; 

compiletoram
