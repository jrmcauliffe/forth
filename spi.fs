\res MCU: MSP430F5510

\res export POUT PDIR PREN PDS PSEL0 PSEL1 UCB1CTL0 UCB1CTL1 UCSWRST UCSYNC UCB1TXBUF UCB1RXBUF UCB1BR0 UCB1BR1
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
  dup 0= if 2drop else \ skip if zero arg command
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

160 Constant ROWS
128 Constant COLS 
128 Constant MAXITER

\ View of complex plane
0     -1 2Constant IMIN
0      1 2Constant IMAX
$3FFF -2 2Constant RMIN \ -1.75
$BFFF  0 2Constant RMAX \ 1.75

: zmap ( n1 n2 -- df1 df2) \ Map (row, col) to point in complex view z (r, i)
  swap 0 -rot 0 swap       \ convert to fixed point and swap row and col
  RMAX RMIN d- 0 ROWS f/ f* RMIN d+ \ scale rows to real component
  2SWAP
  IMAX IMIN d- 0 COLS f/ f* IMIN d+ \ scal cols to imaginary component
;

: lcd-colour ( n -- ) \ write 565 colour

  $00 $00 $00 $7F CASET
  $00 $00 $00 $9F RASET
  RAMWR
  ISDATA io-1! 
  CS io-0!
  ROWS COLS  * 0 do dup 8 rshift >spi dup >spi loop
  CS io-1!
  drop
;

: zdup ( z -- z z ) \ duplicate a complex number
  2over 2over
;

: zdrop ( z -- ) \ drop a complex value
  2drop 2drop
;

: z+ ( z1 z2 -- z3 ) \ add two complex numbers
  2rot d+ 2-rot d+ 2swap
;

: zover ( z1 z2 -- z1 z2 z1 ) \ over for complex number
  7 pick 7 pick 7 pick 7 pick
;

: zsquared  ( z1 -- z2 ) \ square a complex number
  2over 2dup f* 2over 2dup f* d- 
  2-rot f* 0 2 f*  
;

: fz ( z1 z2 -- z3 ) \ z c -> z*z + c
  zover                 \ copy z as double doubles are hard to handle with stack ops
  zsquared z+           \ square and add c
  2rot 2drop 2rot 2drop \ drop initial z
;

: z. ( z -- ) \ print a complex number
  2swap 3 f.n ." + " 3 f.n ." i"
;

: setpixel ( colour row column -- ) \ print 16 bit value to pixel
  $00 swap 2dup CASET
  $00 swap 2dup RASET
  RAMWR
  ISDATA io-1!
  CS io-0!
  dup 8 rshift >spi >spi
  CS io-1!
;

: escaped? ( z -- flag ) \ is absolute value of z < 2?
  2dup f* 2swap 2dup f* d+ 0 4 d>
;

: mandel? ( z -- flag) \ is this complex numberi c in the mandelbrot set?
  \ zdup          \ save copy of original point
  0 0 0 0       \ Initial z
  0  
  MAXITER 0 do
  drop
  zdup escaped? if i leave then \ if escaped, put iterations on stack
  zover fz       \ run iteration f(z) = z*z + c
  i loop         \ leave i on top of stack
  4 0 do -rot 2drop loop \ Get rid of last z and c values from stack leaving i
;

: scale-colour ( n -- n ) \ scale number of iterations to greyscale shade (assume 565 colour)
  MAXITER 64 /
  * dup 1 rshift tuck \ One for each of RGB (565)
  6 lshift or
  5 lshift or
;

: mandlebrot ( colour -- ) \ scan across each row writing a pixel
  lcd-init
  $0000 lcd-colour  \ blank screen
  ROWS 0 do
    COLS 0 do
      \ Grab complex number that represends this row/col
        j i zmap mandel? scale-colour j i setpixel
    loop
  loop
;

: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else mandlebrot then
;

