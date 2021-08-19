compiletoflash

\res MCU: MSP430FR2433

\ DIGITAL_IO
\res export POUT PDIR PREN PDS PSEL0 PSEL1 P1OUT P1DIR P2OUT P2DIR P3OUT P3DIR 

#include ms.fs
#include digital-io.fs

\ Project pin assignments
2 2 io constant p1
1 3 io constant p2
3 0 io constant p3
2 3 io constant p4
3 1 io constant p5
2 4 io constant p6
2 5 io constant p7
2 6 io constant p8
2 7 io constant p9
3 2 io constant p10
2 0 io constant p11
2 1 io constant p12
1 2 io constant p13
1 1 io constant p14
1 0 io constant p15
1 7 io constant p16
1 6 io constant p17

: myinit \ ( -- )
 \ Set pins to ouput and off
 $CF P1OUT cbic!               \ p1.5 & p1.6 reserved for serial connection
 $FF P2OUT cbic!               \ All of p2
 $07 P3OUT cbic!               \ only p3.0, p3.1 & p3.2 on chip 
 $CF P1DIR cbis!               \ p1.5 & p1.6 reserved for serial connection
 $FF P2DIR cbis!               \ All of p2
 $07 P3DIR cbis!               \ only p3.0, p3.1 & p3.2 on chip 

;

: toggle \ (pin -- )
 dup io-1! 1000 ms io-0!   
;

: cycle \ ( -- )
  p1 toggle
  p2 toggle
  p3 toggle
  p4 toggle
  p5 toggle
  p6 toggle
  p7 toggle
  p8 toggle
  p9 toggle
  p10 toggle
  p11 toggle
  p12 toggle
  p13 toggle
  p14 toggle
  p15 toggle
  p16 toggle
  p17 toggle
;

: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else myinit begin cycle again then
; 

compiletoram
