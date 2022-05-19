\res MCU: MSP430F5510

\res export UCB1CTL0 UCB1CTL1 UCSWRST UCSYNC UCB1TXBUF UCB1RXBUF UCB1BR0 UCB1BR1 UCB1STAT
#require digital-io.fs

: spi. ( -- ) \ display SPI hardware registers
;
  
: +spi ( -- ) CS io-0! ;  \ select SPI
: -spi ( -- ) CS io-1! ;  \ deselect SPI

: >spi> ( c -- c )
  begin UCB1STAT @ 0= until UCB1TXBUF c! UCB1RXBUF c@ 
;

: spi> ( -- c ) \ read byte from SPI
  0 >spi>
;

: >spi ( c -- ) \ write byte to SPI
  >spi> drop  
;


: spi-slow ( -- )
  \ Reset UCS
  UCSWRST UCB1CTL1 cbis!
  $00 UCB1BR1 c! 40 UCB1BR0 c! \ 8Mhz -> 200 kHz
  \ Enable UCS
  UCSWRST UCB1CTL1 cbic!
;
: spi-init ( -- )  \ set up hardware SPI
  OUTMODE-SP0 MOSI   io-mode!
  OUTMODE-SP0 MISO   io-mode!
  OUTMODE-HS  CS     io-mode!
  OUTMODE-SP0 SCLK   io-mode!

  \ Reset UCS
  UCSWRST UCB1CTL1 cbis!
  \ Use SMCLK for CLK
  $80 UCB1CTL1 cbis!
  \ SMCLK Full Speed
  $00 UCB1BR1 c! $00 UCB1BR0 c!
  \ Rising sample / MSB / 8 bit / Master / 3 pin / async  
  $A8 UCB1CTL0 c!
  \ Enable UCS
  UCSWRST UCB1CTL1 cbic!
;

