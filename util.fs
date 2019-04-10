\ e4thcom command line buffer
\index  rewind-to-basis

compiletoflash

\ Clock control registers
$056 constant DCOCTL
$057 constant BCSCTL1
$058 constant BCSCTL2
$053 constant BCSCTL3

$176 constant TA0CCR2
$174 constant TA0CCR1
$172 constant TA0CCR0
$170 constant TA0R
$166 constant TA0CCTL2
$164 constant TA0CCTL1
$162 constant TA0CCTL0
$160 constant TA0CTL
$12E constant TA0IV

$196 constant TA1CCR2
$194 constant TA1CCR1
$192 constant TA1CCR0
$190 constant TA1R
$186 constant TA1CCTL2
$184 constant TA1CCTL1
$182 constant TA1CCTL0
$180 constant TA1CTL
$11E constant TA1IV

\ Project pin assignments
: pin 1 swap lshift 1-foldable ; \ Create output pin mask(s)
0 pin constant pgreen \ LED Green p2.0
1 pin constant pblue  \ LED Blue  p2.1
4 pin constant pred   \ LED Red   p2.4
5 pin constant pbutton  \ Rotary encoder button  p1.5
6 pin constant protary1 \ Rotary encoder switch1 p1.6
7 pin constant protary2 \ Rotary encoder switch2 p1.7
\ 2 pin constant ptap     \ Free pin to test timer etc p2.2

$FFFF variable buttonstate
$0 variable rotary1state
$0 variable rotary2state
$0 variable colour
: alloff pgreen pred or pblue or p2out cbic! ;


: setcolour if p2out cbis! else p2out cbic! then ;


: updatecolour
pred colour @ 1 and 0= setcolour
pgreen colour @ 2 and 0= setcolour
pblue colour @ 4 and 0= setcolour
;

: button alloff ;
: cw colour @ 1+ 7 mod 7 and colour ! updatecolour ;
: ccw colour @ 1- 7 mod 7 and colour ! updatecolour ;

: timerA0-irq-handler
buttonstate @ shl pbutton p1in cbit@ 1 and or buttonstate !
buttonstate @ $8000 = if button then
rotary1state @ shl protary1 p1in cbit@ not 1 and or $FE00 or rotary1state !
rotary1state @ $FF00 = rotary1state @ rotary2state @ > and if ccw then
rotary2state @ shl protary2 p1in cbit@ not 1 and or $FE00 or rotary2state !
rotary2state @ $FF00 = rotary2state @ rotary1state @ > and if cw then
;

\ Second Timer_A is referred to as Timer_B in mecrisp interrupt table
: timerB0-irq-handler
;


: myinit
['] timerA0-irq-handler irq-timera0 ! \ register handler for interrupt
$2D0 TA0CTL ! \ SMCLK/8 up mode interrupts not enabled<?>
$90 TA0CCTL0 ! \ toggle mode / interrupts enabled
$1F4 TA0CCR0 ! \ Set to 1Mhz / 500 -> 0.5ms
pbutton protary1 or protary2 or p1ren cbis! \ Enable pullup on pushbutton
\ ptap p2dir cbis! \ Enable test pin for output
pgreen pred or pblue or p2dir cbis! \ Set red, green an blue pins to output
pgreen pred or pblue or p2out cbic! \ Default to all off
eint \ Enable interrupts
;

compiletoram
