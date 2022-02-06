compiletoflash

\res MCU: MSP430FR2355

\ Timer_A0
\res export TA1CTL TA1CCTL0 TA1CCTL1 TA1CCR0 TA1CCR1 TA1EX0
\res export UCA0CTLW0 UCA0BRW UCA0MCTLW UCA0IE UCA0IFG UCA0IV UCA0RXBUF UCA0TXBUF

#include ms.fs
#include digital-io.fs
#include ring.fs

\ Project pin assignments

2 0 io constant audioOut
1 6 io constant midiRx
1 7 io constant midiTx
32 4 + buffer: txbuffer

: init_cv
  OUTMODE-SP0 audioOut io-mode!
  $0210 TA1CTL !   \ SMCLK/8 up mode interrupts not enabled
  $0080 TA1CCTL1 ! \ Toggle Mode / interrupts disabled
  $0007 TA1EX0 !   \ Divide by a further 8
  $0FFF TA1CCR0 !  \ TAxCCR0 At 1Mhz -> 20ms
  $0001 TA1CCR1 !  \ Just need a value here for toggle to work
;

: >midi
  txbuffer >ring   \ Right buffer
  $02 UCA0IE cbis! \ enable TX interupt
;

\ : midi>
\   UCA0RXBUF @
\ ;

: midi_handler
  \ Source of UART interrupt
  UCA0IV @ case
    $02 of \ Rx buffer ready
      UCA0RXBUF @ >midi \ forward to output
    endof
    $04 of \ TX buffer ready
       \ Send byte to UART TX buffer
       txbuffer ring> UCA0TXBUF !
       \ If buffer empty, disable TX interrupt
       txbuffer ring# 0= if $02 UCA0IE cbic! then
    endof
  endcase
;

: init_midi
  \ TX Buffer
  txbuffer 32 init-ring

  OUTMODE-SP0 midiRx io-mode!
  OUTMODE-SP0 midiTX io-mode!
  $0001 UCA0CTLW0 cbis!         \ Reset state machine
  $00C0 UCA0CTLW0 cbis!         \ Use SMCLK
  16 UCA0BRW !                  \ 8Mhz SMCLK, 31250 Midi baud rate
  $0001 UCA0MCTLW !             \ enable oversampling
  $0001 UCA0CTLW0 cbic!         \ Initialise state machine
  $0001 UCA0IE cbis!            \ enable RX interrupt
  ['] midi_handler irq-uscia0 ! \ register interrupt handler
  eint                          \ enable interrupts
;

: my_init
   init_midi
;

: note_on  $90 >midi >midi $7A >midi ;
: note_off $90 >midi >midi 0 >midi ;
: note dup note_on 500 ms note_off ;
: multi_test 60 swap - dup 60 swap do i note_on loop 500 ms 60 swap do i note_off loop ;
: Hz 62500 swap  u/mod swap drop 3 lshift ;

: >Speaker TA1CCR0 ! ;

: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else my_init then
;

compiletoram
