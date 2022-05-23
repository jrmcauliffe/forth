\res MCU: MSP430F5510

\res export UCB1CTL0 UCB1CTL1 UCSWRST UCSYNC UCB1TXBUF UCB1RXBUF UCB1BR0 UCB1BR1 UCB1STAT UCBUSY
#require digital-io.fs

: spi. ( -- ) \ display SPI hardware registers
  UCB1STAT @ hex.
;
  
: +spi ( -- ) CS io-0! ;  \ select SPI
: -spi ( -- ) CS io-1! ;  \ deselect SPI

: >spi> ( c -- c )
  begin UCBUSY UCB1STAT cbit@ 0= until
  UCB1TXBUF c!
  begin UCBUSY UCB1STAT cbit@ 0= until
  UCB1RXBUF c@
;

: spi> ( -- c ) \ read byte from SPI
  $FF >spi>
;

: >spi ( c -- ) \ write byte to SPI
  >spi> drop  
;


: spi-slow ( -- )
  \ Reset UCS
  UCSWRST UCB1CTL1 cbis!
  00 UCB1BR1 c! 80 UCB1BR0 c! \ 8Mhz -> 100 kHz
  \ Enable UCS
  UCSWRST UCB1CTL1 cbic!
;
: spi-init ( -- )  \ set up hardware SPI

  \ Reset UCS
  UCSWRST UCB1CTL1 cbis!
  \ Use SMCLK for CLK
  $80 UCB1CTL1 cbis!
  \ SMCLK/2 Full Speed
  00 UCB1BR1 c! 08 UCB1BR0 c!
  \ Mode 0 / MSB / 8 bit / Master / 3 pin / sync
  $A9 UCB1CTL0 c!

  OUTMODE-SP2 MOSI io-mode!
  OUTMODE-SP2 MISO io-mode!
  OUTMODE-HS  CS   io-mode!
  OUTMODE-SP2 SCLK io-mode!
  -spi

  \ Enable UCS
  UCSWRST UCB1CTL1 cbic!

;

