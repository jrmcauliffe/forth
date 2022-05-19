#require ms.fs
#require digital-io.fs
#require spi.fs

\ configure additional pins required by ST7735 module
OUTMODE-LS RESET  io-mode!
OUTMODE-LS ISDATA io-mode!

160  Constant ROWS
128  Constant COLS 

: lcd_cmd ( c n -- ) \ Hex command and number of arguments
  <builds , , align
  does>
  +spi
  ISDATA io-0!         \ Swtch to command
  dup cell+ @ >spi     \ Send command
  -spi
    @ dup              \ Grab argument count and save a copy for cleanup
  dup 0= if 2drop else \ skip if zero arg command
    +spi
    ISDATA io-1!       \ Switch to data
    1 swap do i pick   \ Send in reverse order
      >spi
    -1 +loop
    0 do drop loop     \ Cleanup Stack
    -spi
  then
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

: lcd-colour ( n -- ) \ write 565 colour to whole screen
  $00 $00 $00 $7F CASET
  $00 $00 $00 $9F RASET
  RAMWR
  ISDATA io-1! 
  +spi
  ROWS COLS  * 0 do dup 8 rshift >spi dup >spi loop
  -spi
  drop
;

: setpixel ( colour row column -- ) \ print 16 bit value to pixel
  $00 swap 2dup CASET
  $00 swap 2dup RASET
  RAMWR
  ISDATA io-1!
  +spi
  dup 8 rshift >spi >spi
  -spi
;
