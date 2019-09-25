\res  MCU: MSP430F5510
\res export P4OUT P4DIR P4REN

$200 constant PBASE
$1   constant ODDOFFSET
$20  constant EVENOFFSET
$2   constant POUT 
$4   constant PDIR
$6   constant PREN 
$8   constant PDS 
$A   constant PSEL 


                          \ SEL  PD REN OUT DIR
$1   constant OUTMODE-LS  \   0   0   X   X   1



: io ( port# pin# -- pin ) \ combine port and pin into int
  swap 8 lshift or 2-foldable ;
: io# ( pin -- u ) \ convert pin to bit position 
  $7 and 1-foldable ;
: io-mask ( pin -- u ) \ convert pin to bit mask
  1 swap io# lshift 1-foldable ;
: io-port ( pin -- u ) \ convert pin to port number
  8 rshift 1-foldable ;
: io-base (pin -- addr ) \ convert pin to base address
  io-port 1 - 2 /mod EVENOFFSET * swap ODDOFFSET * + PBASE + ;
: io-mode! ( mode pin -- ) \ Set io mode registers for pin
  dup io-base PDIR + -rot  dup io-mask -rot io# lshift and swap c! ;
: io-split ( pin -- io-mask io-base )
  dup io-mask swap io-base ;
: io@ ( pin -- flag )
  io-split POUT + cbit@ ;
: io-0! ( pin -- ) \ set pin to low
  io-split POUT + cbic! ;
: io-1! ( pin -- ) \ set pin to low
  io-split POUT + cbis! ;
: iox! ( pin -- ) \ set pin to low
  io-split POUT + cxor! ;


: led 4 7 io 0-foldable ;
: led_init OUTMODE-LS led io-mode! ;
