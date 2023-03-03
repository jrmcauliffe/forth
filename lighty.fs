compiletoflash

\res MCU: MSP430FR2433

\ TIMERS
\res export TA0CTL TA0CCTL1 TA0CCTL2 TA0CCR0 TA0CCR1 TA0CCR2
\res export TA1CTL TA1CCTL1 TA1CCTL2 TA1CCR0 TA1CCR1 TA1CCR2
\res export TA2CTL TA2CCTL0 TA2CCR0
\res export TA3CTL TA3CCTL0 TA3CCR0
\res export RTCCTL RTCIV RTCMOD RTCCNT

\ PORT Configuration
\res export P1DIR P1SEL0 P1SEL1 P2OUT P2DIR P2REN

\ Debounce status tracking variables & constants
\ see http://www.ganssle.com/debouncing.htm for details
\ Real work is done in the tick-interrupt-handler
$0000 variable laststate
250 constant d_ticks_per_sec                               \ Number of debounce ticks in a second
64 constant ticks_per_sec                                  \ Number of clock ticks in a second
20 constant origLightLevel
600 constant timeoutSeconds
origLightLevel variable lightLevel                \ The system 'dimmed' value for light

\ circular buffer to store debounce and test port samples
0 variable rp
4 constant rs
rs buffer: ring
: >ring ring rp @ + c! rp @ 1+ rs 1- and rp ! ;
: ringand $FF ring rs + ring do i c@ and loop ;

: writeColor ( r g b -- )
  TA0CCR1 !
  TA1CCR2 !
  TA1CCR1 !
;
\ Fix value between max and min values
: clamp 90 min 0 max ;

: debounce-tick-interrupt-handler
  $0201 c@ not $9F and >ring \ read input port and write to buffer skip uart pins and invert
  ringand laststate @ 2dup not and
  case
    1 of 3 and 0= if ." encoder a left" cr then endof
    2 of 3 and 0= if ." encoder a right" cr then endof
    4 of drop ." button a pressed " cr endof
    8 of 24 and 0= if ." encoder b left" cr then endof
    16 of 24 and 0= if ." encoder b right" cr then endof
    128 of drop ." button b pressed " cr endof
    drop
   endcase

  laststate !
;

: clock-tick-interrupt-handler
  \ 'Close in' on desired value to avoid abrupt light level changes
  \ But scale the shifts so that big jumps don't take forever (pressing the button etc)
  lightLevel @ dup * TA0CCR1 @ \ Calculate the desired TA0CCR1 by squaring desired level
  dup -rot - 3 arshift         \ Find the difference and then divide this by 2^3 (8)
  dup 0= if drop               \ if 0 then we're close enough to assume the desired value
  else + then dup dup writeColor        \ otherwise add offset to close in on desired TA0CCR value
;

: rtc-interrupt-handler
  0 lightLevel ! \ Lights out
  RTCIV @ drop   \ Clear interrupt
  2 RTCCTL bic!  \ Disable interrupt
;

: myinit \ ( -- )
  \ Port 1, Bits 1,4 & 5 are the blue, green & red channels driven by the timers
  $32 DUP DUP P1DIR cbis! P1SEL0 cbic! P1SEL1 cbis!

  \ Port 2, Bits - 0, 1, 2, 3, 4, & 7 are for the encoders, 5 & 6 used by uart so don't change
  \ Set these bits as input with internal pullup
  $9F dup dup P2OUT cbis! P2DIR cbic! P2REN cbis!

  \ Timer A0/A1 for running PWM Lamp / LED dimming duty
  $0008             TA0CTL bis!  \ Set TACLR to clear timer
  $0008             TA1CTL bis!  \ Set TACLR to clear timer
  $1FFF             TA0CCR0 !    \ Frequency
  $1FFF             TA1CCR0 !    \ Frequency
  $0000             TA0CCR1 !    \ Lamp initial duty cycle (tick will move this to lightLevel)
  $0000             TA1CCR1 !    \ Lamp initial duty cycle (tick will move this to lightLevel)
  $0000             TA1CCR2 !    \ Lamp initial duty cycle (tick will move this to lightLevel)
  $00E0             TA0CCTL1 !
  $00E0             TA1CCTL1 !
  $00E0             TA1CCTL2 !
  $210              TA0CTL !     \ SMCLK/1 Start in up mode
  $210              TA1CTL !     \ SMCLK/1 Start in up mode

  \ Timer A2 for switch debounce
  $2D0 TA2CTL !                           \ SMCLK/8 - Up Mode
  1000 d_ticks_per_sec / 1000 * TA2CCR0 ! \ trigger ever ticks_per_sec
  $10  TA2CCTL0 !                         \ Enable interupts

  \ Timer A3 for updating lamp values
  $110 TA3CTL !                         \ ACLK/1 - Up Mode
  $7FFF ticks_per_sec /  TA3CCR0 !
  $10  TA3CCTL0 !                       \ Enable interupts

  \ RTC for timeout
  $3302   RTCCTL !                      \ VCLOCK / 1000 /w interrupts
  timeoutSeconds 10 * RTCMOD !          \ 10 ticks per second
  $0040   RTCCTL bis!

  \ Register interrupt handlers and enable interrupts
  ['] debounce-tick-interrupt-handler irq-timerc0 ! \ (C0 is mecrisp's confusing name for A2 main interrupt)
  ['] clock-tick-interrupt-handler irq-timerd0 !    \ (D0 is mecrisp's confusing name for A3 main interrupt)
  ['] rtc-interrupt-handler irq-rtc !               \ RTC handler

  eint
;


: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else myinit then
; 

compiletoram
