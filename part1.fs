$D0000020 constant GPIO_OE
$D000001C constant GPIO_OUT_XOR

: blink ( n -- )               \ blink n times
  1 25 lshift GPIO_OE !        \ enable output on GP25
  2 * 0 do                     \ double the count as we are toggling on each loop
   1 25 lshift GPIO_OUT_XOR !  \ toggle led
   250 ms                      \ wait
  loop
; 
