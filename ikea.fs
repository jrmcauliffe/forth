compiletoflash

\res MCU: MSP430FR2433

\ DIGITAL_IO
\res export POUT PDIR PREN PDS PSEL0 PSEL1 P1IN P1OUT P1DIR P2OUT P2DIR P3OUT P3DIR 
\ TIMERS
\res export TA0CTL TA0CCTL0 TA0CCTL1 TA0CCTL2 TA0CCR0 TA0CCR1 TA0CCR2 TA1CTL TA1CCTL0 TA1CCTL1 TA1CCTL2 TA1CCR0 TA1CCR1 TA1R
#include ms.fs
#include digital-io.fs


$0 variable buttonstate

\ Project pin assignments
1 1 io constant pLight
1 2 io constant pButton
0 3 io constant pLED


500 constant led_hz
\ Debouncing constants
8 constant debounce_ms
debounce_ms 1000 led_hz / / constant debounce_ticks
$FFFF debounce_ticks 1 - lshift constant debounce_check
debounce_check shl constant debounce_mask

\ Simple squared gamma 0-255, clamp to 30 as lamp malfunctions under that
: light dup 0= if else 255 min 30 max dup *
  then 
    $10 TA0CTL bic! \ stop timer
    TA0CCR1 !        \ write new value
   nop nop nop nop nop
   nop nop nop nop nop
   nop nop nop nop nop
   nop nop nop nop nop
   nop nop nop nop nop
   nop nop nop nop nop
   nop nop nop nop nop
    $10 TA0CTL bis! \ restart timer in up mode
;
    

\ TODO workout why this glitches
: dim 255 begin dup light 1 - 5 ms dup 0= until 0 light ;

:  toggle ( pin -- )
  dup io@ if io-0! else io-1! then
;

\ handle time and rotary decoder
: timerA1-irq-handler
pLed io-1!
\  buttonstate @ shl pButton P1IN cbit@ or debounce_mask or buttonstate !
\  buttonstate @ debounce_check = if pLED toggle then
;

: myinit \ ( -- )
  OUTMODE-HS pLED io-mode!    \ Indicator LED
  OUTMODE-SP1 pLight io-mode! \ Lamp


  \ Timer A0 for running Lamp / led dimming duty
  $210 TA0CTL !     \ SMCLK/1


  $0080 TA0CCTL0 !
  $00E0 TA0CCTL1 !
  $FFFF TA0CCR0 !   
  $7FFF TA0CCR1 !

  \ TODO fix for lower refresh rate
  \ Timer A1 for switch debounce and clock time
  $2D2 TA1CTL !   \ SMCLK/8 - Up Mode - enable interrupts
  $FFFF TA1CCR0 ! \ 16 hz
  $10  TA1CCTL0 ! \ Enable interupts

  \ Register interrupt handlers  
  ['] timerA1-irq-handler irq-timera1 !
  \ Enable interrupts
\ eint 
;


: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else myinit then
; 

compiletoram
