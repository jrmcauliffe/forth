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
$FF variable lightLevel
\ Project pin assignments
1 1 io constant pLight
2 2 io constant pButton
2 3 io constant pLED
3 0 io constant pRotary1
3 1 io constant pRotary2
5 constant step



\ Debouncing constants
5 constant debounce_ms
$FFFF debounce_ms 1 - lshift constant debounce_check
debounce_check shl constant debounce_mask

\ Simple squared gamma 0-255, clamp to 30 as lamp malfunctions under that
: light dup 0= if else 255 min 30 max dup *
  then 
    $200 TA0CTL ! \ stop timer
    TA0CCR1 !     \ write new value
    $210 TA0CTL ! \ restart timer in continous mode
;
    

\ TODO workout why this glitches
: dim 255 begin dup light 1 - 5 ms dup 0= until 0 light ;


\ TODO MAKE THIS WORK
: triggered? \ ( pin var -- flag)
\  dup -rot @ shl io@ or debounce_mask or dup rot ! debounce_check =
\ handle time and rotary decoder
;
: ms-interrupt-handler
 \ pLED iox!
  buttonstate @ shl pButton io@ or debounce_mask or dup buttonstate ! debounce_check = if $FF dup lightLevel ! light  then
  r1state @ shl pRotary1 io@ or debounce_mask or dup r1state !
  r2state @ shl pRotary2 io@ or debounce_mask or dup r2state !
  2dup
  debounce_check = swap debounce_mask = and if lightLevel @ step - dup lightLevel ! light then
  swap
  debounce_check = swap debounce_mask = and if lightLevel @ step + dup lightLevel ! light then



\ pButton buttonState triggered? if pLED iox! then
;

: myinit \ ( -- )
  OUTMODE-HS  pLED     io-mode! \ Indicator LED
  OUTMODE-SP1 pLight   io-mode! \ Lamp
  INMODE-PU   pButton  io-mode! \ Rotary Pushbutton
  INMODE-PU   pRotary1 io-mode! \ Rotary Quadrature 1
  INMODE-PU   pRotary2 io-mode! \ Rotary Quadrature 2

  \ Timer A0 for running Lamp / led dimming duty
  $210 TA0CTL !     \ SMCLK/1
  $0080 TA0CCTL0 !
  $00E0 TA0CCTL1 !
  $FFFF TA0CCR0 !   
  $7FFF TA0CCR1 !

  \ TODO fix for lower refresh rate
  \ Timer A1 for switch debounce and clock time
  $2D0 TA1CTL !   \ SMCLK/8 - Up Mode - disable interrupts
  1000 TA1CCR0 ! \ 25Hz
  $10  TA1CCTL0 ! \ Enable interupts

  \ Register interrupt handlers  
  ['] ms-interrupt-handler irq-timerb0 ! \ (B0 is mecrisp's confusing name for A1)
  \ Enable interrupts
  eint
;


: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else myinit then
; 

compiletoram
