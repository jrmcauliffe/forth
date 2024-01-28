\res MCU: MSP430FR2355

\ Timer_A1
\res export TA1CTL TA1CCTL0 TA1CCTL1 TA1CCR0 TA1CCR1 TA1EX0

\ SAC/DAC
\res export PMMCTL2 SAC0OA SAC0PGA SAC0DAC SAC0DAT SAC0DATSTS SAC0IV


#include digital-io.fs

1 1 io constant dacOut


: init_dac ( -- )
  ANALOGMODE dacOut io-mode!
  $0021 PMMCTL2 !  \ Enable internal voltage reference 2.5v
  $0099 SAC0OA !   \ Reference DAC with PGA
  $0001 SAC0PGA !  \ No gain buffer PGA
  $1001 SAC0DAC !  \ internal voltage reference / enable dac
  $0100 SAC0OA bis! \ Enable OA
  $0400 SAC0OA bis! \ Enable SAC
;

: >dac ( u -- )
  SAC0DAT !
;

