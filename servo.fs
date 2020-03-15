\res MCU: MSP430F5510

\ Timer_A0
\res export TA0CTL TA1CTL TA0CCR1 TA0CCTL1

#include ms.fs
#include digital-io.fs

: servo ( pin, TAxCTL, n -- ) \ Create a servo using io pin and timer base and offset
  <builds , , , align
  does> dup 4 + @ swap dup 2 + @ swap
;

: init_servo ( servo -- )        \ Initialise timers for servo
  rot OUTMODE-SP swap io-mode!   \ set io pin to output special function
  swap dup dup                   \ two copies of timer base  
  $2D0 swap !                    \ SMCLK/8 up mode interrupts not enabled
  20000 swap $12 + !             \ TAxCCR0 At 1Mhz -> 20ms
  2dup 
  swap @ $2 * $02 + + $E0 swap ! \ TAxCCTLt CCI1A /  Rest/Set mode / interrupts disabled
  over @ $2 * $12 + + dup rot !  \ replace timer number with actual TAxCCRy register address for updates
  1500 swap !                    \ TAxCCRy At 1Mhz -> 1.5ms
;

: update_servo ( servo ms -- )   \ set on pulse duration for servo
  swap @ ! drop drop
;
