#include ms.fs


: lcd_strobe  ( -- ) \ Strobe lcd to read nibble
  LCD_E io-1! LCD_E io-0!
;

: ?lcd_busy  ( -- f )
  LCD_RS io-0! LCD_RW io-1!   \ Set lcd to read register
  INMODE-NR LCD_DB7 io-mode!  \ Set DB7 to input
  LCD_E io-1! LCD_DB7 io@     \ Strobe and read busy flag
  LCD_E io-0!
  lcd_strobe                  \ Strobe again for lower nibble
  OUTMODE-LS LCD_DB7 io-mode! \ Set DB7 back to output
  LCD_RW io-0!                \ Set lcd to write register
;


: uppernibble> ( port# -- c )  \ Read upper nibble of port
  8 lshift io-base POUT +      \ calculate address
  c@ 4 rshift                  \ read
;

: >uppernibble  ( char port# -- ) \ write upper nibble of port
  8 lshift io-base POUT +         \ calculate address
  dup c@ $0F and                  \ save lower nibble
  rot 4 lshift or swap c!         \ join and write byte 
;

: >lcdnibble ( c -- ) \ write char to lcd port upper nibble
  LCD_RW io-0!
  LCD_PORT >uppernibble
  lcd_strobe
;
  
: >lcdbyte ( c -- )         \ Write a byte to the lcd
  dup 4 rshift  >lcdnibble  \ Send upper nibble to lcd
  >lcdnibble                \ Send lower nibble to lcd
  LCD_DB7 io-1!             \ Set busy flag
;

: >lcdi ( u -- )            \ Write config byte to lcd
  LCD_RS io-0!              \ set to instruction
  begin ?lcd_busy not until \ Wait until not busy
  >lcdbyte                  \ send to lcd
;

: >lcd ( c -- ) \ Write a char to lcd
  LCD_RS io-1!  \ set to data
  >lcdbyte      \ send to lcd
;

: lcd_clear ( -- ) \ Clear the lcd screen
  $1 >lcdi
;
: lcd_init ( -- ) \ Initialise registers needed by lcd
  OUTMODE-LS LCD_POWER io-mode!
  OUTMODE-LS LCD_RS io-mode!
  OUTMODE-LS LCD_RW io-mode!
  OUTMODE-LS LCD_E io-mode!
  OUTMODE-LS LCD_DB4 io-mode!
  OUTMODE-LS LCD_DB5 io-mode!
  OUTMODE-LS LCD_DB6 io-mode!
  OUTMODE-LS LCD_DB7 io-mode!
  LCD_POWER io-0!             \ Toggle power
  LCD_POWER io-1!
  500 ms                      \ Wait time for boot
  LCD_RS io-0!                \ Set to intsruction mode
  $3 >lcdnibble  20 ms        \ Function set (8 bit, 1 line)
  $3 >lcdnibble 300 us        \ Function set (8 bit, 1 line)
  $3 >lcdnibble 300 us        \ Function set (8 bit, 1 line)
  $2 >lcdnibble 300 us        \ Function set (4 bit, 1 line)
  $20 >lcdi                   \ Function set (4 bit, 1 line)
  $0c >lcdi                   \ Display on
  $01 >lcdi                   \ Clear Display
;
