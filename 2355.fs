\res  MCU: MSP430FR2355
\res  export POUT PDIR PREN PDS PSEL0 PSEL1 SAC0OA SAC0PGA SAC0DAC SAC0DAT SAC1OA SAC1PGA SAC1DAC SAC1DAT PMMCTL2
 
#include ms.fs
#include digital-io.fs

1 0 io CONSTANT LED0
1 1 io CONSTANT DAC0
1 5 io CONSTANT DAC1
: myinit
  OUTMODE-HS LED0 io-mode! 
  OUTMODE-SP2 DAC0 io-mode!
  OUTMODE-SP2 DAC1 io-mode!
  $0061 PMMCTL2 !       \ enable internal voltage reference 2.5v
  $0599 SAC0OA !        \ enable SAC0 with DAC
  $0011 SAC0PGA !       \ Set Gain to unity/buffer
  $1001 SAC0DAC !       \ Enable and configure DAC with internal Ref
  $0599 SAC1OA !        \ enable SAC1 with DAC
  $0011 SAC1PGA !       \ Set Gain to unity/buffer
  $1001 SAC1DAC !       \ Enable and configure DAC with internal Ref
;

myinit 

: mv>dac ( u -- u )     \ scale a mV value to DAC const ;
  4096 7500 */
;

: >dac0  ( u -- )        \ write a value to the DAC
  mv>dac SAC0DAT !
;

: >dac1  ( u -- )        \ write a value to the DAC
  mv>dac SAC1DAT !
;

: midi>mv ( u -- u)     \ convert a midi note number to standard mV value
  36 - 1000 12 */ dup 0< if drop 0 then
;

: midi>dac ( u -- )     \ write a midi note to the DAC
  midi>mv mv>dac
;
: led_on LED0 io-1! ;
: led_off LED0 io-0! ;
