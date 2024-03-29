compiletoflash

: cornerstone ( Name ) ( -- )
  <builds begin here $1FF and while 0 , repeat
  does>   begin dup  $1FF and while 1+  repeat eraseflashfrom
;

: u.4 ( u -- ) 0 <# # # # # #> type ;
: u.2 ( u -- ) 0 <# # # #> type ;

: hexdump ( -- ) \ Dumps complete Flash
  cr hex

  \ MSP430F2274: Complete: $FFFF $8000
  \ MSP430G2553: Complete: $FFFF $C000

  $FFFF $C000 \ Complete image with Dictionary
  do
    \ Check if it would be $FFFF only:
    0                 \ Not worthy to print
    i #16 + i do      \ Scan data
      i c@ $FF <> or  \ Set flag if there is a non-$FF byte
    loop

    if
      ." :10" i u.4 ." 00" \ Write record-intro with 4 digits.
      $10                   \ Begin checksum
      i    $FF and +        \ Sum the address bytes 
      i >< $FF and +        \ separately into the checksum

      i #16 + i do
        i c@ u.2 \ Print data with 2 digits
        i c@ +     \ Sum it up for Checksum
      loop

      negate u.2  \ Write Checksum
      cr
    then

  #16 +loop
  ." :00000001FF" cr
  decimal
; 

: us 0 ?do [ $3C00 , $3C00 , ] loop ;
: ms 0 ?do 998 us loop ;

cornerstone Rewind-to-Basis

\ Unlike ANS-Marker, the defined "Rewind-to-Basis" stays in Flash and doesn't remove itself upon invocation. 
\ I think, this should be more useful for its given task to conserve some basics.

compiletoram

