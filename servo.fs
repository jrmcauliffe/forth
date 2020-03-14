\res MCU: MSP430F5510
\res export P4OUT P4DIR P4REN

\ Timer_A0
\res export TA0CTL TA0CCTL1 TA0CCTL2 TA0CCTL3 TA0CCTL4 TA0CCR0 TA0CCR1 TA0CCR2 TA0CCR3 TA0CCR4

#include ms.fs
#include digital-io.fs

1 2 io CONSTANT servo1
1 3 io CONSTANT servo2
1 4 io CONSTANT servo3
1 5 io CONSTANT servo4

: init_servo ( -- ) \ Initialise timers for servo
  OUTMODE-SP servo1 io-mode!
  OUTMODE-SP servo2 io-mode!
  OUTMODE-SP servo3 io-mode!
  OUTMODE-SP servo4 io-mode!
  $2D0  TA0CTL !   \ SMCLK/8 up mode interrupts not enabled
  \ Base timer
  20000 TA0CCR0 !  \ At 1Mhz -> 20ms
  \ Servo 1
  1500  TA0CCR1 !  \ At 1Mhz -> 1.5ms
  $E0   TA0CCTL1 ! \ CCI1A / Reset/Set mode / interrupts disabled
  \ Servo 2
  1500  TA0CCR2 !  \ At 1Mhz -> 1.5ms
  $E0   TA0CCTL2 ! \ CCI1A / Reset/Set mode / interrupts disabled
  \ Servo 3
  1500  TA0CCR3 !  \ At 1Mhz -> 1.5ms
  $E0   TA0CCTL3 ! \ CCI1A / Reset/Set mode / interrupts disabled
  \ Servo 4
  1500  TA0CCR4 !  \ At 1Mhz -> 1.5ms
  $E0   TA0CCTL4 ! \ CCI1A / Reset/Set mode / interrupts disabled
;
