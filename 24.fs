\res MCU: MSP430FR2355
\ \res export CSCTL0 CSCTL1 CSCTL2 CSCTL3 CSCTL4 CSCTL5 CSCTL6 CSCTL7 CSCTL8
\ #include speed.fs

\ output mclock on p3.0
\ 3 0 io constant clk
\ OUTMODE-SP0 clk io-mode!

\ set xtal pins to SP-1
\ 2 6 io constant xout
\ 2 7 io constant xin

$1 $022A c! $1 $0224 c!

: twofour ( -- )
$3F $0205 c!  \ Set P2 direction
$c0 $020D c!  \ Set P2 pins to special Xtal mode
$08EC $018C ! \ Set CSCTL6 to HF internal oscillator 24MHZ
$0102 $0188 ! \ Set CSCTL4 to SELMS xtal

\ 100 ms 
begin
$0002 $018E bic! \ reset xtal fault
$0002 $0102 bic! \ clear xtal fault intrpt.
$0002 $018E bit@ not until
$0001 $0580 bis! \ **Put state machine in reset**
$000D $0586 !
$2501 $0588 !
$0001 $0580 bic! \ **Put state machine out of reset**


\ 1152 24000 BAUDRATE
;
\ OUTMODE-SP1 xout io-mode!
\ OUTMODE-SP1 xin  io-mode!

