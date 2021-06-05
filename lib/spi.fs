\res MCU: MSP430F5510

\res export UCB1CTL0 UCB1CTL1 UCSWRST UCSYNC UCB1TXBUF UCB1RXBUF UCB1BR0 UCB1BR1
#require digital-io.fs

: spi. ( -- ) \ display SPI hardware registers
;
  
: +spi ( -- ) ;  \ select SPI
: -spi ( -- ) ;  \ deselect SPI

\ : >spi> ( c -- c )  \ hardware SPI, 8 bits
\  SPI1-DR !  begin SPI1-SR @ 1 and until  SPI1-DR @
\ ;

\ single byte transfers
: spi> ( -- c ) UCB1RXBUF c@ ;  \ read byte from SPI
: >spi ( c -- ) \ write byte to SPI
  UCB1TXBUF c!
;

: spi-init ( -- )  \ set up hardware SPI
  OUTMODE-LS  RESET  io-mode!
  OUTMODE-SP0 MOSI   io-mode!
  OUTMODE-LS  CS     io-mode!
  OUTMODE-SP0 SCLK   io-mode!
  OUTMODE-LS  ISDATA io-mode!

  \ Reset UCS
  UCSWRST UCB1CTL1 c!
  \ Use SMCLK for CLK
  $80 UCB1CTL1 cbis!
  \ SMCLK Full Speed
  $00 UCB1BR1 c! $00 UCB1BR0 c!
  \ Rising sample / MSB / 8 bit / Master / 3 pin / Sync  
  $A9 UCB1CTL0 c!
  \ Enable UCS
  UCSWRST UCB1CTL1 cbic!
;
