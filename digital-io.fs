$200 constant PBASE
$1   constant ODDOFFSET
$20  constant EVENOFFSET
$02  constant POUT
$04  constant PDIR
$06  constant PREN
$08  constant PDS
$0A  constant PSEL
                         \ SEL1 SEL0  DS REN DIR OUT
$00 constant INMODE-NR   \    0    0   0   0   0   X  Input
$04 constant INMODE-PD   \    0    0   0   1   0   0  Input with pulldown resistor
$05 constant INMODE-PU   \    0    0   0   1   0   1  Input with pulldown resistor
$02 constant OUTMODE-LS  \    0    0   0   X   1   X  Output with reduced drive strength
$0B constant OUTMODE-HS  \    0    0   1   X   1   1  Output with high drive strength
$12 constant OUTMODE-SP0 \    0    1   0   X   1   X  Ouput reduced with special function 0
$22 constant OUTMODE-SP1 \    1    0   0   X   1   X  Ouput reduced with special function 1
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
  swap 14 2 DO 2dup $1 AND 0= if io-split i + cbic! else io-split i + cbis! then shr 2 +loop 2drop ;
: io@  ( pin -- flag )
  io-split cbit@ ;
: io-0!  ( pin -- ) \ set pin to low
  io-split POUT + cbic! ;
: io-1!  ( pin -- ) \ set pin to high
  io-split POUT + cbis! ;
: io!  ( ? pin -- ) \ if true, set pin high else low
  swap if io-1! else io-0! then ;
: iox!  ( pin -- ) \ Toggle pin value
  io-split POUT + cxor! ;
