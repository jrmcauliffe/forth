\ configure additional pins required by SH1106 module
OUTMODE-LS RESET  io-mode!
OUTMODE-LS ISDATA io-mode!

64   Constant ROWS
128  Constant COLS 
ROWS 3 rshift CONSTANT PAGES
ROWS COLS * 8 / CONSTANT FRAMESIZE
FRAMESIZE PAGES / CONSTANT PAGESIZE
FRAMESIZE BUFFER: FRAMEBUFFER

: lcd_cmb_cmd ( c -- ) \ Hex command with lower bits or'd
  <builds , align
  does>
  -spi
  ISDATA io-0!         \ Swtch to command
  +spi 
  @ or >spi     \ Send command
  -spi
  ISDATA io-1!
;
: lcd_cmd ( c n -- ) \ Hex command and number of arguments
  <builds , , align
  does>
  -spi
  ISDATA io-0!         \ Swtch to command
  +spi dup cell+ @ >spi     \ Send command
  -spi
    @ dup              \ Grab argument count and save a copy for cleanup
  dup 0= if 2drop else \ skip if zero arg command
    1 swap do i pick   \ Send in reverse order
      +spi >spi -spi
    -1 +loop
    0 do drop loop     \ Cleanup Stack
  then
  ISDATA io-1!         \ Switch to data
;

$00 lcd_cmb_cmd LCOL                  \ CMD 1
$10 lcd_cmb_cmd HCOL                  \ CMD 2 
$30 lcd_cmb_cmd SETPUMPV              \ CMD 3
$40 lcd_cmb_cmd SETSTARTLINE          \ CMD 4
$81 1 lcd_cmd SETCONTRAST             \ CMD 5
$A0 lcd_cmb_cmd SETSEGREMAP           \ CMD 6 
$A4 lcd_cmb_cmd SETDISPLAYALLON       \ CMD 7
$A6 lcd_cmb_cmd SETINVERSEDISPLAY     \ CMD 8
$A8 1 lcd_cmd SETMULTIPLEXRATIO       \ CMD 9   
$8D 1 lcd_cmd SETCHARGEPUMP           \ CMD 10
$AE lcd_cmb_cmd SETDISPLAY            \ CMD 11
$B0 lcd_cmb_cmd SETPAGEADD            \ CMD 12
$C0 lcd_cmb_cmd SETSCANDIR            \ CMD 13
$D3 1 lcd_cmd SETDISPLAYOFFSET        \ CMD 14
$D5 1 lcd_cmd SETDISPLAYCLOCKDIV      \ CMD 15
$D9 1 lcd_cmd SETPRECHARGE            \ CMD 16
$DA 1 lcd_cmd SETCOMSIGPADS           \ CMD 17
$DB 1 lcd_cmd SETVCOMDESEL            \ CMD 18

: blankBuffer ( -- ) \ clear framebuffer
  FRAMESIZE 0 DO $AA FRAMEBUFFER I + c! LOOP 
;

: writeBuffer ( -- ) \ send frambuffer to screen
  PAGES 0 do
    i SETPAGEADD    
    -spi 
    ISDATA io-1! 
    +spi
    128 0 do
      0 >spi
    loop
    -spi
  loop
;

: init_lcd
  init_spi
  RESET io-1! 1 ms
  RESET io-0! 10 ms
  RESET io-1! 
  0 SETDISPLAY                    \ Display off
  2 LCOL
  0 HCOL
  3 SETPUMPV      \ 9v
  0 SETSTARTLINE
  0 SETPAGEADD                    \ Page address 0
  $80 SETCONTRAST
  0 SETSEGREMAP  
  0 SETINVERSEDISPLAY
  $3F SETMULTIPLEXRATIO
  $14 SETCHARGEPUMP
  1 3 lshift SETSCANDIR           \ Scan direction in bit3
  $00 SETDISPLAYOFFSET
  $80 SETDISPLAYCLOCKDIV
  $1F SETPRECHARGE
  $12 SETCOMSIGPADS
  $40 SETVCOMDESEL                \ Recommended val on carrier datasheet
  0 SETDISPLAYALLON
  \ blankBuffer
  \ writeBuffer
  1 SETDISPLAY                     \ Display on
;
