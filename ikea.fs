compiletoflash

\res MCU: MSP430FR2433

\ DIGITAL_IO
\res export POUT PDIR PREN PDS PSEL0 PSEL1 P1OUT P1DIR P2OUT P2DIR P3OUT P3DIR 
\ TIMERS
\res export TA0CTL TA0CCTL0 TA0CCTL1 TA0CCTL2 TA0CCR0 TA0CCR1 TA0CCR2 TA2R TA2CTL TA2CCTL0 TA2CCTL1 TA2CCR0 TA2CCR1
#include ms.fs
#include digital-io.fs

\ Project pin assignments
1 1 io constant pLight
: myinit \ ( -- )
  OUTMODE-HS pLight io-mode! \ Blue LED
  pLight io-1!               \ Turn on
;
: toggle pLight io-1! dup ms pLight io-0! ms ;
: flash begin dup toggle key? until drop ;
: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else myinit then
; 

compiletoram
