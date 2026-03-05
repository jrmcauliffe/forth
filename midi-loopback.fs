compiletoflash

\res MCU: MSP430FR2355

\ Serial Port
\res export UCA0CTLW0 UCA0BRW UCA0MCTLW UCA0IE UCA0IFG UCA0IV UCA0RXBUF UCA0TXBUF

#include ms.fs
#include digital-io.fs
#include ring.fs

\ Project pin assignments
1 6 io constant midiRx
1 7 io constant midiTx

64 4 + buffer: monbuffer

\ MIDI state machine variables
0 variable midi-state   \ 0=idle, 1=wait data1, 2=wait data2
0 variable midi-status
0 variable midi-data1

: data-count ( status -- n )
  $F0 and case
    $80 of 2 endof  $90 of 2 endof
    $A0 of 2 endof  $B0 of 2 endof
    $C0 of 1 endof  $D0 of 1 endof
    $E0 of 2 endof
    $F0 of
      midi-status @ case
        $F2 of 2 endof  $F3 of 1 endof
        0 swap
      endcase
    endof
    0 swap
  endcase
;

: .ch     ." ch:" midi-status @ $0F and 1+ . ;
: .note-off  ( vel -- )  cr ." NoteOff  " .ch ."  note:" midi-data1 @ .  ."  vel:" . ;
: .note-on   ( vel -- )
  dup 0= if .note-off exit then
  cr ." NoteOn   " .ch ."  note:" midi-data1 @ .  ."  vel:" . ;
: .cc        ( val -- )  cr ." CC       " .ch ."  ctrl:" midi-data1 @ .  ."  val:" . ;
: .pc        ( prg -- )  cr ." ProgChg  " .ch ."  prog:" . ;
: .pressure  ( val -- )  cr ." Pressure " .ch ."  val:" . ;
: .aftertouch ( val -- ) cr ." Aftertouch " .ch ."  note:" midi-data1 @ .  ."  val:" . ;
: .pitchbend  ( msb -- ) cr ." PitchBend " .ch ."  val:" midi-data1 @ . . ;

: .system ( -- )
  midi-status @ case
    \ $F8 of cr ." Clock"    endof   \ suppressed: 24/beat floods output
    $FA of cr ." Start"    endof
    $FB of cr ." Continue" endof
    $FC of cr ." Stop"     endof
    $FF of cr ." Reset"    endof
  endcase
;

: dispatch ( byte -- )
  midi-status @ $F0 and case
    $80 of .note-off   endof
    $90 of .note-on    endof
    $A0 of .aftertouch endof
    $B0 of .cc         endof
    $C0 of .pc         endof
    $D0 of .pressure   endof
    $E0 of .pitchbend  endof
    $F0 of drop .system endof
    drop
  endcase
;

: midi-parse ( byte -- )
  dup $80 and if                        \ status byte
    midi-status !
    midi-status @ $F0 and $F0 = if      \ system message?
      midi-status @ data-count 0= if .system then
    else
      1 midi-state !                    \ channel message: wait for first data byte
    then
  else                                  \ data byte
    midi-state @ case
      1 of
        midi-status @ $F0 and $C0 =
        midi-status @ $F0 and $D0 = or if
          dispatch  0 midi-state !      \ 1-data message: dispatch and reset
        else
          midi-data1 !  2 midi-state !  \ first of 2: store and wait for second
        then
      endof
      2 of
        dispatch  0 midi-state !        \ second data byte: dispatch and reset
      endof
      drop                              \ state 0: ignore unexpected data byte
    endcase
  then
;

: midi_handler
  UCA0IV @ case
    $02 of  \ Rx buffer ready
      UCA0RXBUF @ dup $FE <> if
        dup UCA0TXBUF !
        monbuffer >ring
      else drop then
    endof
  endcase
;

: monitor ( -- )  \ Print received MIDI bytes as hex until keypress
  begin
    monbuffer ring# if monbuffer ring> midi-parse then
    key?
  until key drop
;

: init_midi
  monbuffer 64 init-ring
  0 midi-state !
  OUTMODE-SP0 midiRx io-mode!
  OUTMODE-SP0 midiTx io-mode!
  $0001 UCA0CTLW0 cbis!         \ Reset state machine
  $00C0 UCA0CTLW0 cbis!         \ Use SMCLK
  48 UCA0BRW !                  \ 24MHz SMCLK, 31250 MIDI baud rate
  $0001 UCA0MCTLW !             \ Enable oversampling
  $0001 UCA0CTLW0 cbic!         \ Initialise state machine
  $0001 UCA0IE cbis!            \ Enable RX interrupt
  ['] midi_handler irq-uscia0 ! \ Register interrupt handler
  eint                          \ Enable interrupts
;

: init ( -- )
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else init_midi then
;

compiletoram
