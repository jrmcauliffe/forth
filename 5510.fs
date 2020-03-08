\res  MCU: MSP430F5510
\res export P4OUT P4DIR P4REN

$200 constant PBASE
$1   constant ODDOFFSET
$20  constant EVENOFFSET
$02  constant POUT
$04  constant PDIR
$06  constant PREN
$08  constant PDS
$0A  constant PSEL
                        \ SEL  DS REN DIR OUT
$0 constant INMODE-NR   \   0   0   0   0   X  Input
$4 constant INMODE-PD   \   0   0   1   0   0  Input with pulldown resistor
$5 constant INMODE-PU   \   0   0   1   0   1  Input with pulldown resistor
$2 constant OUTMODE-LS  \   0   0   X   1   X  Output with reduced drive strength
$9 constant OUTMODE-HS  \   0   1   X   1   1  Output with high drive strength

: io  ( port# pin# -- pin ) \ combine port and pin into int
  swap 8 lshift or 2-foldable ;
: io#  ( pin -- u ) \ convert pin to bit position
  $7 and 1-foldable ;
: io-mask  ( pin -- u ) \ convert pin to bit mask
  1 swap io# lshift 1-foldable ;
: io-port  ( pin -- u ) \ convert pin to port number
  8 rshift 1-foldable ;
: io-base  ( pin -- addr ) \ convert pin to base address
  io-port 1 - 2 /mod EVENOFFSET * swap ODDOFFSET * + PBASE + 1-foldable ;
: io-split  ( pin -- io-mask io-base )
  dup io-mask swap io-base 1-foldable ;
: io-mode!  ( mode pin -- ) \ Set io mode registers for pin using constants
  swap 12 2 DO 2dup $1 AND 0= if io-split i + cbic! else io-split i + cbis! then shr 2 +loop 2drop ;
: io@  ( pin -- flag )
  io-split POUT + cbit@ ;
: io-0!  ( pin -- ) \ set pin to low
  io-split POUT + cbic! ;
: io-1!  ( pin -- ) \ set pin to high
  io-split POUT + cbis! ;
: io!  ( ? pin -- ) \ if true, set pin high else low
  swap if io-1! else io-0! then ;
: iox!  ( pin -- ) \ Toggle pin value
  io-split POUT + cxor! ;

: us 0 ?do [ $3C00 , $3C00 , ] loop ;
: ms 0 ?do 998 us loop ;

\ Onboard green led
4 7 io CONSTANT LED
: led_init OUTMODE-LS LED io-mode! ;
: led_on LED io-1! ;
: led_off LED io-0! ;

\ Onboard lcdi
1 CONSTANT LCD_PORT
5 1 io CONSTANT LCD_POWER
LCD_PORT 1 io CONSTANT LCD_RS
LCD_PORT 2  io CONSTANT LCD_RW
LCD_PORT 3 io CONSTANT LCD_E
LCD_PORT 4 io CONSTANT LCD_DB4
LCD_PORT 5 io CONSTANT LCD_DB5
LCD_PORT 6 io CONSTANT LCD_DB6
LCD_PORT 7 io CONSTANT LCD_DB7



: lcd_busy?  ( -- f )
  LCD_RS io-0! LCD_RW io-1! LCD_E io-1! \ Set to Read Busy
  LCD_DB7 io@  \ Read Busy Flag
  LCD_E io-0!
;

: uppernibble> ( port# -- c )  \ Read upper nibble of port
  8 lshift io-base POUT +      \ calculate address
  c@ 4 rshift                  \ read
;

: .o LCD_PORT uppernibble> hex. ;

: >uppernibble  ( char port# -- ) \ write upper nibble of port
  8 lshift io-base POUT +         \ calculate address
  dup c@ $0F and                  \ save lower nibble
  rot 4 lshift or swap c!         \ join and write byte 
  .o
;

: >lcdnibble ( c -- ) \ write upper nibble to lcd if instruction else data
  LCD_RW io-0!
  LCD_PORT >uppernibble
  LCD_E io-1!
  10 ms
  LCD_E io-0!
;
  
: >lcdf ( c -- ) \ Write a byte to the lcd
  dup 4 rshift  >lcdnibble \ send upper nibble to lcd
  10 ms
  >lcdnibble               \ send lower nibble to lcd
  \ LCD_DB7 io-1!
;

: >lcdi ( u -- ) \ Write config byte to lcd
\  begin 10 us lcd_busy? not until
  LCD_RS io-0!  \ set to instruction
  >lcdf         \ send to lcd
;

: >lcd ( c -- ) \ Write a char to lcd
  LCD_RS io-1!  \ set to data
  >lcdf         \ send to lcd
;

: lcd_init ( -- ) \ Initialise all registers needed by lcd
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
  200 ms                       \ Wait time for boot
  LCD_RS io-0!                \ Set to intsruction mode
  $3 >lcdnibble  50 ms         \ Function set (8 bit, 1 line)
  $3 >lcdnibble 10 ms        \ Function set (8 bit, 1 line)
  $3 >lcdnibble 10 ms        \ Function set (8 bit, 1 line)
  $2 >lcdnibble 10 ms         \ Function set (4 bit, 1 line)
  $20 >lcdi 10 ms
  $0c >lcdi 10 ms             \ Display on
  $01 >lcdi 10 ms              \ Clear Display
;
