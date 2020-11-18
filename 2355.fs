\res  MCU: MSP430FR2355
\res  export POUT PDIR PREN PDS PSEL0 PSEL1 SAC0OA SAC0PGA SAC0DAC SAC0DAT PMMCTL2
 
#include ms.fs
#include digital-io.fs

1 0 io CONSTANT LED0
1 1 io CONSTANT DAC0

: myinit
  OUTMODE-HS LED0 io-mode! 
  OUTMODE-SP2 DAC0 io-mode!
  $0061 PMMCTL2 !       \ enable internal voltage reference 2.5v
  $0599 SAC0OA !        \ enable SAC0 with DAC
  $0011 SAC0PGA !       \ Set Gain to unity/buffer
  $1001 SAC0DAC !       \ Enable and configure DAC with internal Ref
;

myinit 

: >dac  ( u -- )        \ write a value to the DAC
  SAC0DAT !
;


: led_on LED0 io-1! ;
: led_off LED0 io-0! ;
