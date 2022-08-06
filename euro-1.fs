compiletoflash

\res MCU: MSP430FR2355

\ Timer_A0
\res export TA1CTL TA1CCTL0 TA1CCTL1 TA1CCR0 TA1CCR1 TA1EX0
\ Timer_A2
\res export TA2CTL TA2CCTL0 TA2CCTL1 TA2CCR0 TA2CCR1 TA2EX0

\ Serial Port
\res export UCA0CTLW0 UCA0BRW UCA0MCTLW UCA0IE UCA0IFG UCA0IV UCA0RXBUF UCA0TXBUF
\ SAC/DAC
\res export PMMCTL2 SAC0OA SAC0PGA SAC0DAC SAC0DAT SAC0DATSTS SAC0IV

#include ms.fs
#include digital-io.fs
#include sintable.fs

\ Project pin assignments

5 0 io constant audioOut
1 1 io constant dacOut

0 variable accumulator
$FFF variable magic



: init_dac
  ANALOGMODE dacOut io-mode!
  $0021 PMMCTL2 !  \ Enbale internal voltage reference 2.5v
  $0099 SAC0OA !   \ Reference DAC with PGA
  $0001 SAC0PGA !  \ No gain buffer PGA
  $1001 SAC0DAC !  \ internal voltage reference / enable dac
  $0100 SAC0OA bis! \ Enable OA
  $0400 SAC0OA bis! \ Enable SAC
;

: .sac
  cr ." SAC0OA  " SAC0OA @ hex.
  cr ." SAC0PGA " SAC0PGA @ hex.
  cr ." SAC0DAC " SAC0DAC @ hex.
  cr ." SAC0DAT " SAC0DAT @ hex.
  cr
;

: >dac ( u -- )
  SAC0DAT !
;

: sample
 accumulator @ magic @ + dup accumulator ! 6 rshift 1 lshift sintable + @ >dac
 \ accumulator @ magic @ + dup accumulator ! 4 rshift  >dac
;

: init_sampler
  OUTMODE-SP0 audioOut io-mode!
  $0210 TA2CTL !   \ SMCLK/1 up mode interrupts not enabled
  $0090 TA2CCTL0 ! \ Toggle Mode / interrupts enabled
  $0000 TA2EX0 !   \ No further division
  500 TA2CCR0 !    \ 8 kHz
  \ 1 TA2CCR1 !      \ Just need a value here for toggle to work
  ['] sample irq-timerc0 !
;

: sweep 5000 100 do i magic ! 1 ms loop  0 magic ! ;


: my_init
  init_dac
  init_sampler
  eint
;

: Hz 62500 swap  u/mod swap drop 3 lshift ;

: >Speaker TA1CCR0 ! ;

: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else my_init then
;

compiletoram
