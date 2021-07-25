compiletoflash

\res MCU: MSP430FR2433

\ DIGITAL_IO
\res export POUT PDIR PREN PDS PSEL0 PSEL1 P1OUT P1DIR P2OUT P2DIR P3OUT P3DIR 
\ ADC
\res export SYSCFG2 ADCCTL0 ADCCTL1 ADCCTL2 ADCMCTL0 ADCMEM0
\ TIMERS
\res export TA0CTL TA0CCTL0 TA0CCTL1 TA0CCTL2 TA0CCR0 TA0CCR1 TA0CCR2 TA2R TA2CTL TA2CCTL0 TA2CCTL1 TA2CCR0 TA2CCR1
\ CRC (Random Numbers)
\res export CRC16DI CRCDIRB CRCINIRES CRCRESR
#include ms.fs
#include digital-io.fs

\ Project pin assignments
1 2 io constant pBlue
1 1 io constant pGreen
1 3 io constant pAnalog
3 0 io constant pAnalogPower
100 constant darkcutout
300 constant delta
500 constant timeout
0 variable remaining
false variable twinkle
1024 variable lastsample

: sample \ ( -- u )  Sample pin
  pAnalogPower io-1! \ Power voltage divider
  $0012 ADCCTL0 BIS! \ Turn on
  $0001 ADCCTL0 BIS! \ Sample
  begin $01 ADCCTL1 BIT@ not until \ Wait until busy flag cleared
  ADCMEM0 @
  $0012 ADCCTL0 BIC! \ Turn off
  pAnalogPower io-0! \ Power off voltage divider
;

: rand ( -- u ) \ Return psuedorandom number based on 10kHz timer feed through crc
  TA2R @ CRC16DI ! CRCINIRES c@
  1 rshift dup *            \ scale down and square for cheap gamma
;

: tick-irq-handler
  \ If on, then decrement timer
  twinkle @ if
    remaining @ 1- remaining !
    rand dup TA0CCR1 !
    TA0CCR2 !
  else
    0 TA0CCR1 !
    0 TA0CCR2 !
  then \ TWINKLE!
  \ if timed out reset
  remaining @ 0 = if
    false twinkle !
    sample dup dup darkcutout < swap lastsample @ swap - delta > and if
      true twinkle !
      timeout remaining !
    else
      false twinkle !
    then
    lastsample !
  then
;


: myinit \ ( -- )
  \ Configure ADC
  $0110 ADCCTL0 !       \ turn ADC on but leave conversion off
  $0200 ADCCTL1 !       \ Use ADCSC bit for sample trigger
  $0014 ADCCTL2 !       \ 10 bit Slow sample rate
  $0003 ADCMCTL0 !      \ VCC + VSS, A3
  $0008 SYSCFG2 BIS!    \ Set P1.3 to Analog A3 input
  sample lastsample !   \ Set baseline light level

  \ Configure CRC for psuedo random numbers (Good enough for twinkles anyhow)
  \ TODO switch to temp adc based seed
  $FFFF CRCINIRES !

  \ Configure IO
  OUTMODE-SP1 pBlue io-mode!        \ Blue LED
  OUTMODE-SP1 pGreen io-mode!       \ Green LED
  OUTMODE-LS  pAnalogPower io-mode! \ Powers LDR voltage divider
  ANALOGMODE  pAnalog io-mode!      \ Reads LDR voltage divider

  \ Configure Timers

  \ TA0 - LED PWM driver
  $210  TA0CTL !      \ SMCLK/1 up mode interrupts not enabled
  $FFFF TA0CCR0 !     \ Set CCR0 for desired led refresh rate
  $0080 TA0CCTL0 !    \ CCI1B / set\reset mode / interrupts disabled
  $00E0 TA0CCTL1 ! \
  $00E0 TA0CCTL2 ! \ TODO workout optimal timings / implement Red
  $0000 TA0CCR1 !
  $0000 TA0CCR2 !

  \ TA2 - One interrupt per second
  $0110 TA2CTL !   \ ACLK source, Up mode interrupts
  $7FF TA2CCR0 !  \  less than 1 sec from ACLK
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
