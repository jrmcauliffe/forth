\res export TA0CTL TA1CTL TA0CCR1 TA0CCTL1

#include digital-io.fs

\ Configure servo type parameters
\ Range is degrees the servo rotates between standard 1ms and 2ms pulse length
\ Percentage is the overdrive from 1ms to 2ms standard range
\ i.e. 150% will push pulse length range from 750us to 2250us
\ 90 150 servo_type TG9e
: servo_type ( range percentage -- )
  <builds swap 1000 swap / , 500 100 */ , align
  does> dup @ swap 2 + @
;

\ Configure a servo instance on io port using timer CCR n using a servo type
\ 1 2 io TA0CTL 1 TG9e servo servo1
\ will create a servo on P1.2 using the first CCR on TA0 and using the TG9e config
: servo ( io, TAxCTL, n, servo_type ) \ Create a servo using io pin and timer base and offset
  <builds , , , , , align
  does> dup 8 + @ swap dup 6 + @ swap dup 4 + swap dup 2 + @ swap @
;


: init_servo ( servo -- )        \ Initialise timers for servo
  drop drop                      \ ignore timig config for setup
  rot OUTMODE-SP0 swap io-mode!  \ set io pin to output special function
  swap dup dup                   \ two copies of timer base  
  $2D0 swap !                    \ SMCLK/8 up mode interrupts not enabled
  20000 swap $12 + !             \ TAxCCR0 At 1Mhz -> 20ms
  2dup 
  swap @ $2 * $02 + + $E0 swap ! \ TAxCCTLt CCI1A /  Rest/Set mode / interrupts disabled
  over @ $2 * $12 + + dup rot !  \ replace timer number with actual TAxCCRy register address for updates
  1500 swap !                    \ TAxCCRy At 1Mhz -> 1.5ms
;


: clamp ( n min max -- n ) \ Return n clamped to min and max values
  rot min max
;

: degrees ( servo degrees -- ) \ Rotate servo a given number of degrees
  rot * 1500 + swap 1500 over - swap 1500 + clamp
  swap @ ! drop drop
;
