compiletoflash

\res MCU: MSP430FR2355

\ Timer_A2
\res export TA2CTL TA2CCTL0 TA2CCTL1 TA2CCR0 TA2CCR1 TA2EX0

\ SAC/DAC
\res export PMMCTL2 SAC0OA SAC0PGA SAC0DAC SAC0DAT SAC0DATSTS SAC0IV


#require digital-io.fs
#require sintable.fs

1 1 io constant dacOut
5 0 io constant sampleOut

$0 variable accumulator
$0 variable tuning


: init_dac ( -- )
  ANALOGMODE dacOut io-mode!
  $0021 PMMCTL2 !   \ Enable internal voltage reference 2.5v
  $0099 SAC0OA !    \ Reference DAC with PGA
  $0001 SAC0PGA !   \ No gain buffer PGA
  $1001 SAC0DAC !   \ internal voltage reference / enable dac
  $0100 SAC0OA bis! \ Enable OA
  $0400 SAC0OA bis! \ Enable SAC
;

: >dac ( u -- )
  SAC0DAT !
;

: sample
 accumulator @ tuning @ + dup accumulator ! 6 rshift 1 lshift sintable + @ >dac
;

: sweep 5000 100 do i tuning ! 1 ms loop  0 tuning ! ;

: init_sampler
  OUTMODE-SP0 sampleOut io-mode! \ Enbable output on P5.0 to check sample rate
  $0210 TA2CTL !                 \ SMCLK/1 up mode interrupts not enabled
  $0090 TA2CCTL0 !               \ Toggle Mode / interrupts enabled
  $0000 TA2EX0 !                 \ No further division
  500 TA2CCR0 !                  \ 8 kHz
  \ 1 TA2CCR1 !                  \ Just need a value here for toggle to work
  ['] sample irq-timerc0 !       \ Register interrupt handler
;

: init_player
  init_dac
  init_sampler
  eint
;

: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else init_player cr then
;

compiletoram
