\res export POUT PDIR PREN PDS PSEL0 PSEL1

$200 constant PBASE
$1   constant ODDOFFSET
$20  constant EVENOFFSET
                         \ SEL1 SEL0  DS REN DIR OUT
$00 constant INMODE-NR   \    0    0   0   0   0   X  Input
$04 constant INMODE-PD   \    0    0   0   1   0   0  Input with pulldown resistor
$05 constant INMODE-PU   \    0    0   0   1   0   1  Input with pullup resistor
$30 constant ANALOGMODE  \    1    1   X   X   X   X  Analog
$02 constant OUTMODE-LS  \    0    0   0   X   1   X  Output with reduced drive strength
$0A constant OUTMODE-HS  \    0    0   1   X   1   1  Output with high drive strength
$12 constant OUTMODE-SP0 \    0    1   X   X   1   X  Ouput with special function 0
$22 constant OUTMODE-SP1 \    1    0   X   X   1   X  Ouput with special function 1
$32 constant OUTMODE-SP2 \    1    1   X   X   1   X  Ouput with special function 2


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

: io-mode! ( mode pin -- ) \ Set io mode registers for pin using constants
  swap
  2dup $1 AND swap io-split POUT  + rot 0= if cbic! else cbis! then shr
  2dup $1 AND swap io-split PDIR  + rot 0= if cbic! else cbis! then shr
  2dup $1 AND swap io-split PREN  + rot 0= if cbic! else cbis! then shr
  2dup $1 AND swap io-split PDS   + rot 0= if cbic! else cbis! then shr
  2dup $1 AND swap io-split PSEL0 + rot 0= if cbic! else cbis! then shr
  2dup $1 AND swap io-split PSEL1 + rot 0= if cbic! else cbis! then shr
  2drop
 ;

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
