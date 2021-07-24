compiletoflash

\res MCU: MSP430FR2433

\ DIGITAL_IO
\res export POUT PDIR PREN PDS PSEL0 PSEL1 P1OUT P1DIR P2OUT P2DIR P3OUT P3DIR 
\ ADC
\res export SYSCFG2 ADCCTL0 ADCCTL1 ADCCTL2 ADCMCTL0 ADCMEM0
\ TIMERS
\res export TA2CTL TA2CCTL0 TA2CCTL1 TA2CCR0 TA2CCR1

#include ms.fs
#include digital-io.fs

\ Project pin assignments
1 2 io constant pBlue
1 1 io constant pGreen
1 3 io constant pAnalog
3 0 io constant pAnalogPower

: sample \ ( -- u )  Sample pin
  pAnalogPower io-1!
  $0012 ADCCTL0 BIS! \ turn on
  $0001 ADCCTL0 BIS! \ Sample
  begin $01 ADCCTL1 BIT@ not until \ Wait until busy flag cleared
  ADCMEM0 @
  $0012 ADCCTL0 BIC! \ turn off
  pAnalogPower io-0!
;


: tick-irq-handler
  sample 100 < if pGreen io-1! else pGreen io-0! then
;


: myinit \ ( -- )
  \ Configure ADC
  $0110 ADCCTL0 !       \ turn ADC on but leave conversion off
  $0200 ADCCTL1 !       \ Use ADCSC bit for sample trigger
  $0014 ADCCTL2 !       \ 10 bit Slow sample rate
  $0003 ADCMCTL0 !      \ VCC + VSS, A3
  $0008 SYSCFG2 BIS!    \ Set P1.3 to Analog A3 input

  \ Configure IO
  OUTMODE-HS pBlue io-mode!        \ Blue LED
  OUTMODE-HS pGreen io-mode!       \ Green LED
  OUTMODE-LS pAnalogPower io-mode! \ Powers LDR voltage divider
  ANALOGMODE pAnalog io-mode!      \ Reads LDR voltage divider

  \ Configure Timers
  $0110 TA2CTL !   \ ACLK source, Up mode interrupts
  $7FFF TA2CCR0 !  \ 1 second from ACLK
  $0090 TA2CCTL0 ! \ Toggle mode, interrupts enabled

  \ Per second interrupt
  ['] tick-irq-handler irq-timerc0 !
  eint
;


: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else myinit then
; 

compiletoram
