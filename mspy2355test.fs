compiletoflash

\res MCU: MSP430FR2355

\ DIGITAL_IO
\res export POUT PDIR P1OUT P1DIR P2OUT P2DIR P3OUT P3DIR P4OUT P4DIR P5OUT P5DIR P6OUT P6DIR

#include ms.fs
#include digital-io.fs

\ Project pin assignments
1 7 io constant p1
2 0 io constant p2
2 1 io constant p3
2 2 io constant p4
2 3 io constant p5
4 0 io constant p6
4 1 io constant p7
6 0 io constant p8
6 1 io constant p9
6 2 io constant p10
6 3 io constant p11
6 4 io constant p12
6 5 io constant p13
6 6 io constant p14
4 4 io constant p15
4 5 io constant p16
4 6 io constant p17
4 7 io constant p18
2 4 io constant p19
2 5 io constant p20
1 0 io constant p21
1 1 io constant p22
1 2 io constant p23
1 3 io constant p24
3 0 io constant p25
3 1 io constant p26
3 2 io constant p27
3 3 io constant p28
5 0 io constant p29
5 1 io constant p30
5 2 io constant p31
5 3 io constant p32
5 4 io constant p33
3 4 io constant p34
3 5 io constant p35
3 6 io constant p36
3 7 io constant p37
1 4 io constant p38
1 5 io constant p39
1 6 io constant p40

: myinit \ ( -- )
 \ Set pins to ouput and off
 $FF P1OUT cbic!               \ All of p1
 $3F P2OUT cbic!               \ Pin 2.6 & p2.7 reserved for Xtal
 $FF P3OUT cbic!               \ All of p3
 $F3 P4OUT cbic!               \ P4.2 & p4.3 reserved for serial connection 
 $1F P5OUT cbic!               \ Only first 5 bits of P5 present
 $7F P6OUT cbic!               \ Only first 7 bits of P6 presesnt
 $FF P1DIR cbis!               \ All of p1
 $3F P2DIR cbis!               \ Pin 2.6 & p2.7 reserved for Xtal
 $FF P3DIR cbis!               \ All of p3
 $F3 P4DIR cbis!               \ P4.2 & p4.3 reserved for serial connection 
 $1F P5DIR cbis!               \ Only first 5 bits of P5 present
 $7F P6DIR cbis!               \ Only first 7 bits of P6 presesnt

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
  p18 toggle
  p19 toggle
  p20 toggle
  p21 toggle
  p22 toggle
  p23 toggle
  p24 toggle
  p25 toggle
  p26 toggle
  p27 toggle
  p28 toggle
  p29 toggle
  p30 toggle
  p31 toggle
  p32 toggle
  p33 toggle
  p34 toggle
  p35 toggle
  p36 toggle
  p37 toggle
  p38 toggle
  p39 toggle
  p40 toggle
;

: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else myinit begin cycle again then
; 

compiletoram
