\res MCU: MSP430F5510

\res export UCB1CTL0 UCB1CTL1 UCSWRST UCSYNC UCB1TXBUF UCB1RXBUF UCB1BR0 UCB1BR1
#include ms.fs
#include digital-io.fs

\ SPI Constants
4 0 io CONSTANT RESET
4 1 io CONSTANT MOSI
4 2 io CONSTANT CS
4 3 io CONSTANT SCLK
1 7 io CONSTANT ISDATA

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

: lcd_cmd2
  <builds depth dup , 0 do , loop ALIGN
  does> 
  CS io-0!
  \ First write command byte
  ISDATA io-0! 
  dup dup @ cells + @ >spi 
  \ Now write data bytes
  ISDATA io-1! 
  dup @ 1 - 1 swap do dup i cells + @ >spi -1 +loop DROP 
  CS io-1!
;

: lcd_cmd ( c n -- ) \ Hex command and number of arguments
  <builds , , align
  does>
  CS io-0!            \ Enable
  ISDATA io-0!        \ Swtch to command
  dup cell+ @ >spi    \ Send command
  CS io-1!            \ Disable
  @ dup               \ Grab argument count and save a copy for cleanup
  dup 0= if else      \ skip if zero arg command
    CS io-0!
    ISDATA io-1!      \ Switch to data
    1 swap do i pick  \ Send in reverse order
      >spi
    -1 +loop
    0 do drop loop    \ Cleanup Stack
    CS io-1!            \ Disable
  then
;

: spi-init ( -- )  \ set up hardware SPI
  OUTMODE-LS RESET  io-mode!
  OUTMODE-SP MOSI   io-mode!
  OUTMODE-LS CS     io-mode!
  OUTMODE-SP SCLK   io-mode!
  OUTMODE-LS ISDATA io-mode!

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

$01 0 lcd_cmd SWRESET
$11 0 lcd_cmd SLPOUT 
$B1 3 lcd_cmd FRMCTR1
$B2 3 lcd_cmd FRMCTR2
$B3 6 lcd_cmd FRMCTR3
$C0 3 lcd_cmd PWCTR1
$C1 1 lcd_cmd PWCTR2
$C2 2 lcd_cmd PWCTR3
$C3 2 lcd_cmd PWCTR4
$C4 2 lcd_cmd PWCTR5
$C5 1 lcd_cmd VMCTR1
$36 1 lcd_cmd MADCTL
$3A 1 lcd_cmd COLMOD 
$E0 16 lcd_cmd GMCTRP1
$E1 16 lcd_cmd GMCTRN1
$13 0 lcd_cmd NORON
$28 0 lcd_cmd DISPOFF
$29 0 lcd_cmd DISPON
$2A 4 lcd_cmd CASET
$2B 4 lcd_cmd RASET
$2C 0 lcd_cmd RAMWR
: lcd-init
  spi-init
  RESET io-0! 1 ms
  RESET io-1! 1 ms
  SWRESET 180 ms
  SLPOUT 180 ms
  $01 $2C $2D FRMCTR1
  $01 $2C $2D FRMCTR2
  $01 $2C $2D $01 $2C $2D FRMCTR3
  $A2 $02 $84 PWCTR1
  $C5 PWCTR2
  $0A $00 PWCTR3
  $8A $2A PWCTR4
  $8A $EE PWCTR5
  $0E VMCTR1
  $C8 MADCTL
  $05 COLMOD
  $02 $1C $07 $12 $37 $32 $29 $2D $29 $25 $2B $39 $00 $01 $03 $10 GMCTRP1
  $03 $1D $07 $06 $2E $2C $29 $2D $2E $2E $37 $3F $00 $00 $02 $10 GMCTRN1
  NORON
  DISPON  
;

: lcd-red

  $00 $00 $00 $7F CASET
  $00 $00 $00 $9F RASET
  RAMWR
  ISDATA io-1! 
  CS io-0!
  128 160 * 0 do  $F8 >spi $00 >spi loop
  CS io-1!
;
