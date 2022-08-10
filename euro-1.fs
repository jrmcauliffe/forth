compiletoflash

\res MCU: MSP430FR2355

\ Timer_A1
\res export TA1CTL TA1CCTL0 TA1CCTL1 TA1CCR0 TA1CCR1 TA1EX0

\ SAC/DAC
\res export PMMCTL2 SAC0OA SAC0PGA SAC0DAC SAC0DAT SAC0DATSTS SAC0IV
\ ADC
\res export ADCCTL0 ADCCTL1 ADCCTL2 ADCMCTL0 ADCMEM0 ADCIE ADCIFG

#include ms.fs
#include digital-io.fs

\ Project pin assignments

1 0 io constant adcIn
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

: sample ( -- )
  ADCMEM0 @ >dac
;

: init_adc ( -- )
  OUTMODE-SP2 adcIn io-mode! \ DIR pin is ignored
  $0110 ADCCTL0 !       \ turn on ADC
  $0A14 ADCCTL1 !       \ Use timer_B1.1 for sample trigger, SMCLK
  $0020 ADCCTL2 !       \ 12 bit Slow sample rate, unsigned result
  $0010 ADCMCTL0 !      \ Use Vref and Vss on channel A0
  $0001 ADCIE bis!      \ enable interrupts

  \ Setup timer
  $0210 TA1CTL !   \ SMCLK up mode, interrupts not enabled
  $0060 TA1CCTL0 ! \ Set/Reset Mode / interrupts disabled
  $00E0 TA1CCTL1 ! \ Set/Reset Mode / interrupts disabled
  $0FFF TA1CCR0 !
  $07FF TA1CCR1 !
  ['] sample irq-adc !

  $0002 ADCCTL0 bis!   \ Enable ADC
;

: my_init
  init_dac
  init_adc
  eint
;

: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else my_init then cr
;

compiletoram
