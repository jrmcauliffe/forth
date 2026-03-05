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
    monbuffer ring# if monbuffer ring> hex. then
    key?
  until key drop
;

: init_midi
  monbuffer 64 init-ring
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
