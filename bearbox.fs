compiletoflash

#include pins
#include ms

\ Clock control alias for colour control
TA1CCR1 constant green
TA1CCR2 constant red
TA0CCR1 constant blue

\ Project pin assignments
: pin 1 swap lshift 1-foldable ;   \ Create output pin mask(s)
1 pin constant pgreen   \ LED Green p2.1
6 pin constant pblue    \ LED Blue  p1.6
4 pin constant pred     \ LED Red   p2.4
7 pin constant pbutton  \ Rotary encoder button  p1.7
4 pin constant protary1 \ Rotary encoder switch1 p1.4
5 pin constant protary2 \ Rotary encoder switch2 p1.5
2 pin constant ptap     \ Free pin to test timer etc p2.2
160 constant tick       \ 1 percent duty cycle time

red variable currentcolour
$0 variable buttonstate
$0 variable rotary1state
$0 variable rotary2state
true variable debugmode

: percent tick * ;

: pin ( n : n ) \ pin between 0 and 100
  dup 0 < if drop 0 then
  dup 100 tick * > if drop 100 tick * then
;

: >dutycycle ! ;
: <dutycycle @ ;

: printstatus 
  ."  ( r :" red <dutycycle tick / . 
  ." g :" green <dutycycle tick / . 
  ." b :" blue <dutycycle tick / . ." ) "
;

: nextcolour \ Cycle through the colours
  currentcolour @ case
    red of green currentcolour ! endof
    green of blue currentcolour ! endof
    blue of red currentcolour ! endof
  endcase
;

: button
  nextcolour
  debugmode @ if ." button" printstatus cr then
;

: cw
  currentcolour @ <dutycycle 2 percent + pin currentcolour @ >dutycycle
  debugmode @ if ." cw    " printstatus cr then ;

: ccw
  currentcolour @ <dutycycle 2 percent - pin currentcolour @ >dutycycle
  debugmode @ if ." ccw   " printstatus cr then
;

: timerA0-irq-handler \ rotary encoder debounce code
  buttonstate @ shl pbutton p1in cbit@ 1 and or buttonstate !
  buttonstate @ $8000 = if button then
  rotary1state @ shl protary1 p1in cbit@ not 1 and or $FF00 or rotary1state !
  rotary1state @ $FF80 = rotary1state @ rotary2state @ > and if cw then
  rotary2state @ shl protary2 p1in cbit@ not 1 and or $FF00 or rotary2state !
  rotary2state @ $FF80 = rotary2state @ rotary1state @ > and if ccw then
;

: colourflash \ Cycle through colours
  100 percent red >dutycycle 500 ms 0 percent red >dutycycle
  100 percent green >dutycycle 500 ms 0 percent green >dutycycle
  100 percent blue >dutycycle 500 ms 0 percent blue >dutycycle
;

: myinit
  pgreen pred or p2dir cbis! pblue p1dir cbis! \ Set red, green an blue pins to output
  pgreen pred or p2sel cbis! pblue p1sel cbis! \ Set red, green an blue pins to special 
  pbutton protary1 or protary2 or p1ren cbis! \ Enable pullup on pushbuttons

  ['] timerA0-irq-handler irq-timera0 ! \ register handler for interrupt
  $210        TA0CTL !   \ SMCLK/1 up mode interrupts not enabled<?>
  100 percent TA0CCR0 !  \ Set to 8Mhz * 16000 -> 2ms
  $90         TA0CCTL0 ! \ toggle mode / interrupts enabled
  $10E0       TA0CCTL1 ! \ CCI1B / set\reset mode / interrupts disabled

  $210        TA1CTL !   \ SMCLK/1 up mode interrupts disabled
  100 percent TA1CCR0 !  \ Set to 8Mhz * 16000 -> 2ms
  $80         TA1CCTL0 ! \ CCI0A / toggle mode / interrupts enabled
  $E0         TA1CCTL1 ! \ CCI1A / set\reset mode / interrupts disabled
  $E0         TA1CCTL2 ! \ CCI2A / set\reset mode / interrupts disabled

  colourflash \ Flash primary colours

  \ Set inital duty cycles
  2 percent red >dutycycle
  2 percent green >dutycycle
  2 percent blue >dutycycle

  eint \ Enable interrupts
;

: init ( -- ) \ Launch program if no keypress after 2 secs
  ." Press <enter> to prevent auto-launch"
  10 0 do ." ." 200 ms key? if leave then loop
  key? if else ."  Launching" cr myinit then
; 

compiletoram
