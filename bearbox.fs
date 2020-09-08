compiletoflash

\res MCU: MSP430G2553

\ Timer_A0
\res export TA0CTL TA0CCTL0 TA0CCTL1 TA0CCR0 TA0CCR1

\ Timer_A1
\res export TA1CTL TA1CCTL0 TA1CCTL1 TA1CCTL2 TA1CCR0 TA1CCR1 TA1CCR2

\ DIGITAL_IO
\res export P1IN P1OUT P1SEL P1DIR P1IE P1IFG P1REN P2SEL P2DIR P2OUT

#include ms.fs

\ Project pin assignments
: pin 1 swap lshift 1-foldable ; \ Create output pin mask(s)
1 pin constant pgreen            \ LED Green p2.1
6 pin constant pblue             \ LED Blue  p1.6
4 pin constant pred              \ LED Red   p2.4
7 pin constant pbutton           \ Rotary encoder button  p1.7
4 pin constant protary1          \ Rotary encoder switch1 p1.4
5 pin constant protary2          \ Rotary encoder switch2 p1.5
2 pin constant ptap              \ Free pin to test timer etc p2.2
true constant debugmode
100 constant tick                \ 1 percent duty cycle time
60000 constant timeout           \ 60 second timeout
$0 variable buttonstate
$0 variable rotary1state
$0 variable rotary2state
0 variable sleeptimer
0 variable red
0 variable green
0 variable blue
red variable currentcolour

: percentscaledwithgamma ( n1, n2 -- n2 ) \ n1 scaled by n2 %
  swap dup *  \ Simple squared gamma calc
  swap 100 */ \ Scaled to compensate for different colour brightness
;

: updateled ( n colourvar -- ) \ Set scaled value for timer constant
  case
    red of 100 percentscaledwithgamma TA1CCR2 ! endof
    green of 25 percentscaledwithgamma TA1CCR1 ! endof
    blue of 70 percentscaledwithgamma TA0CCR1 ! endof
  endcase
;

: >dutycycle ( n colourvar -- ) \ Set desired percent for given led pinning between 0 and 100 %
  swap 100 min 0 max swap
  2dup ! updateled
;

: dutycycle> ( -- n) \ Fetch desired percent for given led
  @
;

: on ( colourvar -- ) \ Turn led on
  100 swap >dutycycle
;

: off ( coulourvar -- ) \ Turn led off
  0 swap >dutycycle
;

: flash ( colourvar -- ) \ Briefly turn on then off
  dup on 200 ms off
;

: sleep? ( -- ) \ should we sleep?
  sleeptimer @ timeout u>
;

: sleep ( -- ) \ Turn off leds and go to sleep
;

: printstatus ( -- ) \ Show current rgb percentage values
  ."  ( r :" red dutycycle> . ." - " TA1CCR2 @ .
  ." g :" green dutycycle> . ." - " TA1CCR1 @ .
  ." b :" blue dutycycle>  . ." - " TA0CCR1 @ . ." ) "
;

: nextcolour \ Cycle through the colours
  currentcolour @ case
    red of green currentcolour ! endof
    green of blue currentcolour ! endof
    blue of red currentcolour ! endof
  endcase
;

: buttonpress
  $0000 sleeptimer !
  nextcolour
\  debugmode if ." button" printstatus cr then
;

: cw
  0 sleeptimer !
  currentcolour @ dutycycle> 5 + currentcolour @ >dutycycle
\  debugmode if ." cw    " printstatus cr then
;
: ccw
  0 sleeptimer !
  currentcolour @ dutycycle> 5 - currentcolour @ >dutycycle
\  debugmode if ." ccw   " printstatus cr then
;

: timerA0-irq-handler \ rotary encoder debounce code
  1 sleeptimer +! \ inc sleep timer then check to see whether we sleep
  
  buttonstate @ shl pbutton P1IN cbit@ 1 and or buttonstate !
  buttonstate @ $8000 = if buttonpress then
  rotary1state @ shl protary1 P1IN cbit@ not 1 and or $FE00 or rotary1state !
  rotary1state @ $FF00 = rotary1state @ rotary2state @ > and if cw then
  rotary2state @ shl protary2 P1IN cbit@ not 1 and or $FE00 or rotary2state !
  rotary2state @ $FF00 = rotary2state @ rotary1state @ > and if ccw then
  sleep? if
  $0000 sleeptimer !
  pgreen pred or P2SEL cbic! pblue P1SEL cbic! \ Turn off special function
  pgreen pred or P2OUT cbic! pblue P1OUT cbic! \ clear
  lpm4 then
;

: port1-irq-handler \ wake from sleep on button
  pbutton P1IFG cbic! \ Clear interrupt flags
  wakeup
  pgreen pred or P2SEL cbis! pblue P1SEL cbis! \ Set red, green an blue pins to special
  debugmode not if lpm1 then
;

: myinit
  pgreen pred or P2DIR cbis! pblue P1DIR cbis! \ Set red, green an blue pins to output
  pgreen pred or P2SEL cbis! pblue P1SEL cbis! \ Set red, green an blue pins to special
  pbutton protary1 or protary2 or P1REN cbis! \ Enable pullup on pushbuttons
  pbutton P1IE cbis! \ Enable interrupts on pushbutton only

  ['] timerA0-irq-handler irq-timera0 ! \ register handler for timer interrupt
  ['] port1-irq-handler irq-port1 ! \ register handler for timer interrupt
  $210       TA0CTL !   \ SMCLK/1 up mode interrupts not enabled
  100 tick * TA0CCR0 !  \ Set to 8Mhz / 10000 -> 0.8 ms
  $90        TA0CCTL0 ! \ toggle mode / interrupts enabled
  $10E0      TA0CCTL1 ! \ CCI1B / set\reset mode / interrupts disabled

  $210       TA1CTL !   \ SMCLK/1 up mode interrupts disabled
  100 tick * TA1CCR0 !  \ Set to 8Mhz 10000 -> 0.8 ms
  $80        TA1CCTL0 ! \ CCI0A / toggle mode / interrupts enabled
  $E0        TA1CCTL1 ! \ CCI1A / set\reset mode / interrupts disabled
  $E0        TA1CCTL2 ! \ CCI2A / set\reset mode / interrupts disabled

  \ Flash primary colours
  red flash
  green flash
  blue flash

  \ Set inital duty cycles
  10 red >dutycycle
  10 green >dutycycle
  10 blue >dutycycle

  eint \ Enable interrupts
  debugmode not if lpm1 then \ Put into low power mode if not debugging
;

: init ( -- ) \ Launch program if no keypress after 1 sec
  ." Press <enter> for console"
  10 0 do ." ." 100 ms key? if leave then loop
  key? if else myinit then
; 

compiletoram
