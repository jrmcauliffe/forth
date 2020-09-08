\res MCU: MSP430F2433

\ Timer_A0
\res export TA0CTL TA0CCTL0 TA0CCTL1 TA0CCR0 TA0CCR1

#include ms.fs
#include digital-io.fs


\ Project pin assignments

1 1 io constant outpin

: init_cv
  OUTMODE-SP1 outpin io-mode!
  $210 TA0CTL !   \ SMCLK/8 up mode interrupts not enabled
  $E0 TA0CCTL1 !  \ Reset/Set Mode / interrupts disabled
  3314 TA0CCR0 !  \ TAxCCR0 At 1Mhz -> 20ms
  500 TA0CCR1 !  \ 50% duty cycle
;

: bleeps ( pause count -- )
  0 DO dup ms 2000 TA0CCR1 ! dup ms 3000 TA0CCR1 ! loop
  drop
;

: octaves
  4 0 DO i 1000 * TA0CCR1 ! 1000 ms loop ;
