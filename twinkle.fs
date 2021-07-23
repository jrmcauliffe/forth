compiletoflash

\res MCU: MSP430FR2433

\ DIGITAL_IO
\res export POUT PDIR PREN PDS PSEL0 PSEL1 P1OUT P1DIR P2OUT P2DIR P3OUT P3DIR 

#include ms.fs
#include digital-io.fs

\ Project pin assignments
1 2 io constant pBlue
1 1 io constant pGreen
1 3 io constant pAnalog
3 0 io constant pAnalogPower

: myinit \ ( -- )
 OUTMODE-HS pBlue io-mode!
 OUTMODE-HS pGreen io-mode!
 OUTMODE-LS pAnalogPower io-mode!
 pAnalogPower io-1!
;

: toggle \ (pin -- )
 dup io-1! 1000 ms io-0!   
;

: cycle \ ( -- )
  pBlue toggle
  pGreen toggle
;

: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if myinit else myinit then
; 

compiletoram
