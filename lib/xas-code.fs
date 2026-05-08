\ Glue for the e4thcom MSP430 cross-assembler.
\ Terminal sends:  code <name>   <opcode> ,   ...   end-code
\ Each line runs on the target. The header must be open while in interpret
\ state so each ',' deposits raw machine code into the new word's body.

: code      ( "name" -- )   :  postpone [ ;
: end-code  ( -- )          ] postpone ;  ;
