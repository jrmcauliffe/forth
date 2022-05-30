compiletoflash

\ words for simplifying rp2040 io etc
#require playground.4th

\ onboard pico led on gpio pin 25
pin25 constant led

\ Rotary encoder
pin15 constant encButton 
pin17 constant encR1
pin18 constant encR2

encButton input
encR1 input
encR2 input

2000 constant msPerTick
1000 msPerTick * constant usPerTick

$E000E100 constant NVIC_ISER
$E000E180 constant NVIC_ICER

$40054000    constant T_BASE
T_BASE $10 + constant TIMER_ALARM0
T_BASE $20 + constant TIMER_ARMED
T_BASE $34 + constant TIMER_ITR
T_BASE $38 + constant TIMER_ITE
T_BASE $3c + constant TIMER_ITF



: timer0-handler

 
 \ update alarm value (which rearms timer)
 TIMER_ALARM0 @ usPerTick + TIMER_ALARM0 !

 \ Do work 
 ." Timer 0 fired " cr

 \ clear interrupt
 1 TIMER_ITR bis!
;

: init-alarm
 \ Register Handler
  ['] timer0-handler irq-TIMER_0 ! 
  \ Enable interrupts
  dint
  1 TIMER_ITE bis!
   \ Set first alarm value
  TIMERAWL @ usPerTick + TIMER_ALARM0 !
  1 NVIC_ISER bis!
  eint
;

compiletoram

