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
encButton PullUp padOn
encR1 input
encR1 PullUp padOn
encR2 input
encR2 PullUp padOn



\ Debounce status tracking variables & constants
\ see http://www.ganssle.com/debouncing.htm for details
\ Real work is done in the timer0-handler
$0 variable buttonstate                                    \ Encoder bounce tracker
$0 variable r1state                                        \ Encoder rotation 1 tracker
$0 variable r2state                                        \ Encoder rotation 2 tracker
8 constant debounce_ms                                     \ Settle time for switch debounce
$FFFFFFFF debounce_ms 1 - lshift   constant debounce_check \ Constant for tracking debounce
debounce_check shl                 constant debounce_mask  \ Constant for tracking debounce

1    constant msPerTick
1000 msPerTick * constant usPerTick

$E000E100 constant NVIC_ISER
$E000E180 constant NVIC_ICER

$40054000    constant T_BASE
T_BASE $10 + constant TIMER_ALARM0
T_BASE $20 + constant TIMER_ARMED
T_BASE $34 + constant TIMER_ITR
T_BASE $38 + constant TIMER_ITE
T_BASE $3c + constant TIMER_ITF

: readpin ( pin -- t/f )
  GPIO_IN @ and 0<>
;

: timer0-handler
 
 \ update alarm value (which rearms timer)
 TIMER_ALARM0 @ usPerTick + TIMER_ALARM0 !

 \ Do work 
 buttonstate @ shl encButton readpin or debounce_mask or dup buttonstate !
   debounce_check = if
   ." Button " cr
 then
  r1state @ shl encR1 readPin or debounce_mask or dup r1state !
  r2state @ shl encR2 readPin or debounce_mask or dup r2state !
  2dup
  debounce_check = swap debounce_mask = and
  -rot swap
  debounce_check = swap debounce_mask = and
  if ." Right" cr then
  if ." Left" cr then
 \ clear interrupt
 1 TIMER_ITR bis!
;

: init-encoder
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

