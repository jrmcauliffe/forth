compiletoflash

\res MCU: MSP430FR2355

\ Timer_A2
\res export TA2CTL TA2CCTL0 TA2CCTL1 TA2CCR0 TA2CCR1 TA2EX0

\ SAC/DAC
\res export PMMCTL2 SAC0OA SAC0PGA SAC0DAC SAC0DAT SAC0DATSTS SAC0IV

#require digital-io.fs
#require sintable.fs

1 1 io constant dacOut       \ DAC IO pin config
32 constant srkHz            \ Sample rate of DAC output in kHz
$0 variable accumulator      \ Store phase as 16 bit unsigned value
$0 variable tuning           \ Distance to advance through the phase accumulator per sample



: >dac ( u -- )              \ Write 12 bit voltage value to DAC pin
  SAC0DAT !
;

: sample ( -- )              \ Interrupt handler to update DAC
 accumulator @ tuning @ +    \ Advance accumulator by tuning word
 dup accumulator !           \ Update accumulator
 6 rshift                    \ Grab upper 10 bits of accumulator
 1 lshift                    \ Double to account for 16 bit values in lookup
 sintable + @ >dac           \ Lookup correct sin value and write to DAC
;

: hz ( u -- u )              \ Convert hertz value to increment for phase accumulator
  $FFFF srkHz 1000 * u*/
;

: sweep ( -- )               \ Sweep through audio from 100 Hz to 1 kHz
  1000 100 do                \ In 1 Hz increments
    i hz tuning ! 10 ms      \ Play for 10ms
  loop
  0 tuning !                 \ Silence
;

: init_dac ( -- )            \ Initialise DAC/SAC hardware
  ANALOGMODE dacOut io-mode! \ Correct IO config
  $0021 PMMCTL2 !            \ Enable internal voltage reference 2.5v
  $0099 SAC0OA !             \ Reference DAC with PGA
  $0001 SAC0PGA !            \ No gain buffer PGA
  $1001 SAC0DAC !            \ Internal voltage reference / enable dac
  $0100 SAC0OA bis!          \ Enable OA
  $0400 SAC0OA bis!          \ Enable SAC
;

: init_sampler ( -- )        \ Initialise timer and attach interrupt handler
  $0210 TA2CTL !             \ SMCLK/1 up mode interrupts not enabled
  $0002 TA2EX0 !             \ 24MHz -> Divide by 3 -> 8 MHz timer clock
  $0090 TA2CCTL0 !           \ Toggle Mode / interrupts enabled
  8000 srkHz / TA2CCR0 !     \ Set sample rate
  ['] sample irq-timerc0 !   \ Register interrupt handler
;


: init_player ( -- )         \ Hardware setup / enable interrupts
  init_dac
  init_sampler
  eint
;

: init ( -- )                \ Launch program if no keypress after 2 sec
  ." Press <enter> for console"
  10 0 do
    ." ." 200 ms key? if leave then
  loop
  key? if else init_player cr then
;

compiletoram
