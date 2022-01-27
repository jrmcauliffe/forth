\res MCU: MSP430FR2355

\ Timer_A0
\res export TA1CTL TA1CCTL0 TA1CCTL1 TA1CCR0 TA1CCR1 TA1EX0

#include ms.fs
#include digital-io.fs


\ Project pin assignments

2 0 io constant outpin

: init_cv
  OUTMODE-SP0 outpin io-mode!
  $0210 TA1CTL !   \ SMCLK/8 up mode interrupts not enabled
  $0080 TA1CCTL1 ! \ Toggle Mode / interrupts disabled
  $0007 TA1EX0 !   \ Divide by a further 8
  $0FFF TA1CCR0 !  \ TAxCCR0 At 1Mhz -> 20ms
  $0001 TA1CCR1 !  \ Just need a value here for toggle to work
;

: Hz 62500 swap  u/mod swap drop 3 lshift ;

: >Speaker TA1CCR0 ! ;
