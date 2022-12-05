\ Clock System
\res export CSCTL1 CSCTL2 CSCTL7

\ Serial Port for speed change
\res export UCA1CTLW0 UCA1BRW UCA1MCTLW

 disable-fll ( -- ) [ $D032 , $0040 , ] inline ; \ Set   SCG0  Opcode bis #40, r2
: enable-fll  ( -- ) [ $C032 , $0040 , ] inline ;  \ Clear SCG0  Opcode bic #40, r2


: KHZ ( ud -- ) \ Set clock speed 
  disable-fll
  \ pick correct DorSel value and write
  dup 1500  <= if 0 else  
  dup 3000  <= if 1 else
  dup 6000  <= if 2 else
  dup 10000 <= if 3 else
  dup 14000 <= if 4 else
  dup 18000 <= if 5 else
  dup 22000 <= if 6 else 7
  then then then then then then then
  1 lshift CSCTL1 ! 
  \ Calculate REFOCLK Multiplier and apply 
  1000 32768 u*/ CSCTL2 ! 
  nop            \ Wait a little bit
  enable-fll
  begin $0300 CSCTL7 bit@ not until \ Wait for FLL to lock
;

: BAUDRATE ( br khz -- ) \ Set registers for request BR (* 100)
  $0001 UCA1CTLW0 bis! \ **Put state machine in reset**
  swap dup rot   \ save a copy of br
  10 um*           \ scale to match baudrate/100
  \ calculate 'n'
  rot um/mod dup 16 <=
  if 
     UCA1BRW !           \ Clear Oversample bit and write UCBRx 
    $1 
  else    
    16 u/mod UCA1BRW !  \ Set oversample and write UCBRx /16
    4 lshift 1 + 
  then
  \ Rot lower half of UCA1MTLW then calculate UCBRSx for fractional portion of n
  -rot 10000 rot u*/
  dup 0 >= swap dup 529 < rot and if $0000 then
  dup 529 >= swap dup 715 < rot and if $0001 then
  dup 715 >= swap dup 835 < rot and if $0002 then
  dup 835 >= swap dup 1001 < rot and if $0004 then
  dup 1001 >= swap dup 1252 < rot and if $0008 then
  dup 1252 >= swap dup 1430 < rot and if $0010 then
  dup 1430 >= swap dup 1670 < rot and if $0020 then
  dup 1670 >= swap dup 2147 < rot and if $0011 then
  dup 2147 >= swap dup 2224 < rot and if $0021 then
  dup 2224 >= swap dup 2503 < rot and if $0022 then
  dup 2503 >= swap dup 3000 < rot and if $0044 then
  dup 3000 >= swap dup 3335 < rot and if $0025 then
  dup 3335 >= swap dup 3575 < rot and if $0049 then
  dup 3575 >= swap dup 3753 < rot and if $004A then
  dup 3753 >= swap dup 4003 < rot and if $0052 then
  dup 4003 >= swap dup 4286 < rot and if $0092 then
  dup 4286 >= swap dup 4378 < rot and if $0053 then
  dup 4378 >= swap dup 5002 < rot and if $0055 then
  dup 5002 >= swap dup 5715 < rot and if $00AA then
  dup 5715 >= swap dup 6003 < rot and if $006B then
  dup 6003 >= swap dup 6254 < rot and if $00AD then
  dup 6254 >= swap dup 6432 < rot and if $00B5 then
  dup 6432 >= swap dup 6667 < rot and if $00B6 then
  dup 6667 >= swap dup 7001 < rot and if $00D6 then
  dup 7001 >= swap dup 7147 < rot and if $00B7 then
  dup 7147 >= swap dup 7503 < rot and if $00BB then
  dup 7503 >= swap dup 7861 < rot and if $00DD then
  dup 7861 >= swap dup 8004 < rot and if $00ED then
  dup 8004 >= swap dup 8333 < rot and if $00EE then
  dup 8333 >= swap dup 8464 < rot and if $00BF then
  dup 8464 >= swap dup 8572 < rot and if $00DF then
  dup 8572 >= swap dup 8751 < rot and if $00EF then
  dup 8751 >= swap dup 9004 < rot and if $00F7 then
  dup 9004 >= swap dup 9170 < rot and if $00FB then
  dup 9170 >= swap dup 9288 < rot and if $00FD then
  swap 9288 >= if $00FE then
  8 lshift or  UCA1MCTLW !
  $0001 UCA1CTLW0 bic!  \ **Initialize USCI state machine**
;

: speed ( br khz -- )  
  \ Set system clock to new speed
  dup KHZ
  \ Then update the Serial port config to match
  BAUDRATE
;
