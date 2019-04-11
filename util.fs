\ e4thcom command line buffer
\index  rewind-to-basis

\ e4th load device specific constants
\res  MCU: MSP430G2553

compiletoflash

\ Clock control registers
\ $056 constant DCOCTL
\ $057 constant BCSCTL1
\ $058 constant BCSCTL2
\ $053 constant BCSCTL3
\ 
\ $176 constant TA0CCR2
\ $174 constant TA0CCR1
\ $172 constant TA0CCR0
\ $170 constant TA0R
\ $166 constant TA0CCTL2
\ $164 constant TA0CCTL1
\ $162 constant TA0CCTL0
\ $160 constant TA0CTL
\ $12E constant TA0IV
\ 
\ $196 constant TA1CCR2
\ $194 constant TA1CCR1
\ $192 constant TA1CCR0
\ $190 constant TA1R
\ $186 constant TA1CCTL2
\ $184 constant TA1CCTL1
\ $182 constant TA1CCTL0
\ $180 constant TA1CTL
\ $11E constant TA1IV

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
$10 constant tick       \ 1 percent duty cycle time

red variable currentcolour
$0 variable buttonstate
$0 variable rotary1state
$0 variable rotary2state
true variable debugmode

: us 0 ?do [ $3C00 , $3C00 , ] loop ;
: ms 0 ?do 998 us loop ;

: percent tick * ;
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
  currentcolour @ <dutycycle 1 percent + currentcolour @ >dutycycle
  debugmode @ if ." cw    " printstatus cr then ;

: ccw
  currentcolour @ <dutycycle 1 percent - currentcolour @ >dutycycle
  debugmode @ if ." ccw   " printstatus cr then
;

: timerA0-irq-handler
  buttonstate @ shl pbutton p1in cbit@ 1 and or buttonstate !
  buttonstate @ $8000 = if button then
  rotary1state @ shl protary1 p1in cbit@ not 1 and or $FE00 or rotary1state !
  rotary1state @ $FF00 = rotary1state @ rotary2state @ > and if cw then
  rotary2state @ shl protary2 p1in cbit@ not 1 and or $FE00 or rotary2state !
  rotary2state @ $FF00 = rotary2state @ rotary1state @ > and if ccw then
;

: myinit
  ['] timerA0-irq-handler irq-timera0 ! \ register handler for interrupt
  $2D0 TA0CTL ! \ SMCLK/8 up mode interrupts not enabled<?>
  $90 TA0CCTL0 ! \ toggle mode / interrupts enabled
  $1F4 TA0CCR0 ! \ Set to 1Mhz / 500 -> 0.5ms
  $10E0 TA0CCTL1 ! \ CCI1B / set\reset mode / interrupts disabled

  $2D0 TA1CTL ! \ SMCLK/8 up mode interrupts disabled
  $60 TA1CCTL0 ! \ CCI0A / toggle mode / interrupts enabled
  $E0 TA1CCTL1 ! \ CCI1A / set\reset mode / interrupts disabled
  $E0 TA1CCTL2 ! \ CCI2A / set\reset mode / interrupts disabled
  $1F4 TA1CCR0 ! \ Set to 1Mhz / 2500 -> 0.5ms

  \ Set inital duty cycles
  1 percent red >dutycycle
  1 percent green >dutycycle
  1 percent blue >dutycycle

  pbutton protary1 or protary2 or p1ren cbis! \ Enable pullup on pushbuttons
  pgreen pred or p2dir cbis! pblue p1dir cbis! \ Set red, green an blue pins to output
  pgreen pred or p2sel cbis! pblue p1sel cbis! \ Set red, green an blue pins to special 

  eint \ Enable interrupts
;

compiletoram
