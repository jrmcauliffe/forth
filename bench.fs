\ Speed bench for shift-isr — TA1 free-running at SMCLK/1 (1 cycle/tick).
\ #include from a debouncer file after shift-isr is defined; then `bench`
\ at the prompt.

\res MCU: MSP430FR2355
\res export TA1CTL TA1R

$220 TA1CTL !                  \ SMCLK + /1 + continuous mode (MC=10)

: bench ( -- )
  TA1R @ shift-isr TA1R @ swap -
  cr ." ticks: " u. cr
;

: empty-bench ( -- )
  TA1R @ TA1R @ swap -
  cr ." ticks: " u. cr
;
