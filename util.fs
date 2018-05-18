compiletoflash

\ Clock control

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

: pin 1 swap lshift ;


: percent $FFFF um* 100 um/mod swap drop ; 

: red! TA1CCR0 ! ;
: green! TA1CCR1 ! ;
: blue! TA1CCR2 ! ;

: .red TA1CCR0 @ 100 $FFFF u*/ 1 + . ;
: .green TA1CCR1 @ 100 $FFFF u*/  1 + . ;
: .blue TA1CCR2 @ 100 $FFFF u*/ 1 + . ;

\ : pwm-init

\  0 pin 1 pin or 5 pin or  p2sel cbis! \ Timer special function
\  0 pin 1 pin or 5 pin or  p2dir cbis! \ Output 
\  $0090 TA1CCTL0 ! \ toggle ie
\  $0090 TA1CCTL1 ! \ toggle ie
\  $0090 TA1CCTL2 ! \ toggle ie

\  50 percent red!
\  50 percent green!
\  50 percent blue!

\  $22 TA1CTL ! \ SMCLK/1, continuous mode, interrupt enabled 
\ ;

: simple \ Light red led on P1.0

6 pin p1sel cbis! \ Timer special funciton
6 pin p1dir cbis! \ Output

$80 TA0CCTL1 ! \ toggle no interrupts
$220 TA0CTL !
;
compiletoram
