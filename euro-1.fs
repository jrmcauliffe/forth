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

\ ------------------------------------
\ SPI Functions
\ ------------------------------------

\ Use UCB1
\res export UCB1CTLW0 UCB1TXBUF UCB1RXBUF UCB1BRW UCB1STATW

3 4 io CONSTANT RESET
4 4 io CONSTANT CS
4 5 io CONSTANT SCLK
4 6 io CONSTANT MOSI
3 0 io CONSTANT ISDATA

:  >spi> ( c -- c )
  begin $0001 UCB1STATW bit@ 0= until
  UCB1TXBUF c!
  begin $0001 UCB1STATW bit@ 0= until
  UCB1RXBUF c@
;

: spi> ( -- c ) \ read byte from SPI
  $FF >spi>
;

: >spi ( c -- ) \ write byte to SPI
  >spi> drop  
;

: +spi CS io-0! ;
: -spi CS io-1! ;


: init_spi ( -- )  \ set up hardware SPI
  $0001 UCB1CTLW0 bis!          \ Reset UCS
  $2983 UCB1CTLW0 !             \ Mode 0 / MSB / 8 bit / Master / 3 pin / sync,  Use SMCLK for CLK
  $00FF UCB1BRW !               \ SMCLK/2 Full Speed
  OUTMODE-SP0 MOSI  io-mode!
  OUTMODE-HS CS     io-mode!
  OUTMODE-SP0 SCLK  io-mode!
  OUTMODE-HS RESET  io-mode!    \ RESET and ISDATA required for SH1106
  OUTMODE-HS ISDATA io-mode!
  CS io-1!
  $0001 UCB1CTLW0 bic!          \ Enable UCS
;

#require SH1106.fs

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
  \ OUTMODE-SP0 2 0 io io-mode!  \ Uncomment to test timer signal on p2.0
  $0210 TA1CTL !   \ SMCLK up mode, interrupts not enabled
  $0060 TA1CCTL0 ! \ Set/Reset Mode / interrupts disabled
  $00E0 TA1CCTL1 ! \ Set/Reset Mode / interrupts disabled
  $00FF TA1CCR0 !
  $007F TA1CCR1 !
  ['] sample irq-adc !

  $0002 ADCCTL0 bis!   \ Enable ADC
;

: my_init
  init_dac
  init_adc
  init_lcd
  eint
;

: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else my_init then cr
;

compiletoram
