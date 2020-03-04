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
: led 4 7 io 0-foldable ;
: led_init OUTMODE-LS led io-mode! ;
: led_on led io-1! ;
: led_off led io-0! ;

\ Onboard lcd
5 1 io CONSTANT LCD_POWER
1 1 io CONSTANT LCD_RS
1 2 io CONSTANT LCD_RW
1 3 io CONSTANT LCD_E
1 4 io CONSTANT LCD_DB4
1 5 io CONSTANT LCD_DB5
1 6 io CONSTANT LCD_DB6
1 7 io CONSTANT LCD_DB7

: .o hex $202 c@ 4 rshift . ;

: >lcd ( c -- ) \ Write a char to the lcd
  $202 2dup
  LCD_RS io-0!
  LCD_RW io-0!
  LCD_E io-1!
  $F0 over cbic! swap $F0 and swap cbis! .o
  LCD_E io-0! LCD_E io-1! \ clock enable line
  $F0 over cbic! swap 4 lshift $F0 and swap cbis! .o
  LCD_E io-0!
;
: >lcdd ( c -- ) \ Write a char to the lcd
  $202 2dup
  LCD_RS io-1!
  LCD_RW io-0!
  LCD_E io-1!
  $F0 over cbic! swap $F0 and swap cbis! .o
  LCD_E io-0!
  LCD_RS io-1!
  LCD_RW io-0!
  LCD_E io-1!
  $F0 over cbic! swap 4 lshift $F0 and swap cbis! .o
  LCD_E io-0!
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
  LCD_POWER io-1!                 \ turn on power
  $31 >lcd 30 us            \ Function set (4 bit, 1 line, small font, IS1)
  $31 >lcd 30 us            \ Function set (4 bit, 1 line, small font, IS1)
  $31 >lcd 30 us            \ Function set (4 bit, 1 line, small font, IS1)
  $21 >lcd 30 us            \ Function set (4 bit, 1 line, small font, IS1)
  $14 >lcd 30 us            \ Set OSC frequency ( ~ 183 Hz)
  $78 >lcd 30 us            \ Set contrast (lower nibble)
  $5E >lcd 30 us            \ Power / ICON ON / Contrast (upper)
  $6A >lcd 200 ms           \ Follower Control
  $0C >lcd 30 us            \ Display on
  $01 >lcd 2 ms             \ Clear Display
  $06 >lcd 30 us            \ Entry Mode
;