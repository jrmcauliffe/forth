compiletoflash                                        \ Save to flash

\res MCU: MSP430FR2433

\ TIMERS
\res export TA0CTL TA0CCTL1 TA0CCTL2
\res export TA0CCR0 TA0CCR1 TA0CCR2
\res export TA1CTL TA1CCTL1 TA1CCTL2
\res export TA1CCR0 TA1CCR1 TA1CCR2
\res export TA2CTL TA2CCTL0 TA2CCR0
\res export TA3CTL TA3CCTL0 TA3CCR0
\res export RTCCTL RTCIV RTCMOD RTCCNT

\ PORT Configuration
\res export P1DIR P1SEL0 P1SEL1
\res export P2IN P2OUT P2DIR P2REN

\ Temporary hack for 16MHZ
: us 0 ?do [ $3C00 , $3C00 , ] loop ;
: ms 0 ?do 1998 us loop ;

$0000           variable laststate
200             constant d_ticks_per_sec              \ Number of debounce ticks in a second
64              constant ticks_per_sec                \ Number of clock ticks in a second
20              constant origLightLevel               \ Default light level on power on / resume from sleep
600             constant timeoutSeconds               \ Timeout to off
origLightLevel  variable rLightLevel                  \ The system 'dimmed' value for red light
origLightLevel  variable gLightLevel                  \ The system 'dimmed' value for green light
origLightLevel  variable bLightLevel                  \ The system 'dimmed' value for blue light
TA1CCR1         constant rTimer                       \ Red CCR register
TA1CCR2         constant gTimer                       \ Green CCR register
TA0CCR1         constant bTimer                       \ Blue CCR register

                                                      \ Create a circular buffer to debounce and test port samples
0               variable rp                           \ Pointer to current ring buffer index
4               constant rs                           \ Size of Ring Buffer (power of 2)
rs              buffer:  ring                         \ Allocate space for Ring buffer

: ringzero ring rs + ring do 0 i c! loop ;            \ Write zeros to all elements in the ring buffer

: >ring ( c -- )
   ring rp @ + c! rp @ 1+ rs 1- and rp !              \ Write a byte to the ring buffer and move pointer
;

: ringAnd ( -- c )
  $FF ring rs + ring do i c@ and loop                 \ Logical and all elements in the ring buffer (for debounce)
;

: writeColor ( r g b -- )                             \ Update timers with correct RGB pwm signal
  TA0CCR1 ! TA1CCR2 ! TA1CCR1 !
;

: clamp 90 min 0 max ;                                \ Fix value between max and min values

: reset-rtc
  $0042 RTCCTL bis!
;
                                                      \ FEATURE FUNCTIONS AND FUNCTION POINTERS
: setLight ( step var -- )
  dup -rot @ + clamp swap !
;


: setWhiteLight ( step -- )
  dup dup
  rLightLevel setLight
  gLightLevel setLight
  bLightLevel setLight
;

: light+ 2 setWhiteLight reset-rtc ;

: light- -2 setWhiteLight reset-rtc ;

: lightsout rLightLevel @ gLightLevel @ and bLightLevel @ and 0=
    if
      origLightLevel dup dup
      rLightLevel !
      gLightLevel !
      bLightLevel !
      reset-rtc
    else 0 dup dup
      rLightLevel !
      gLightLevel !
      bLightLevel !
    then
;

: closeChannel ( timer lvl -- )                       \ Close in on desired value to avoid abrupt light level changes
  swap dup rot                                        \ Save a copy of the timer CCR address for later
  @ dup * 1 lshift swap @                             \ Calculate the desired CCR by squaring desired level and scale
  dup -rot - 3 arshift                                \ Find the difference and then divide this by 2^3 (8)
  dup 0= if drop                                      \ if 0 then we're close enough to assume the desired value
  else + then swap !                                  \ otherwise add offset to close in on desired CCR value
;

: closeIn ( -- )
  rTimer rLightLevel closeChannel
  gTimer gLightLevel closeChannel
  bTimer bLightLevel closeChannel
;

' light-      variable aleft                          \ Default functions called by debounce handler for rotary encoder buttons a & b
' light+      variable aright
' lightsout   variable abutton
' light-      variable bleft
' light+      variable bright
' lightsout   variable bbutton
' nop         variable donothing
' closein     variable tick                           \ Default function called by tick handler

: debounce-tick-interrupt-handler                     \ See http://www.ganssle.com/debouncing.htm
  P2IN c@ not $9F and >ring                           \ Read input port skipping uart pins (5 & 6), invert and write to buffer
  ringAnd laststate @ 2dup not and                    \ Check what's changed from 0->1, copy previous state for rotary encoders
  case                                                \ Call correct function for each input if triggered
    1   of 3 and 0= if aleft @ execute then endof     \ Encoder A Left
    2   of 3 and 0= if aright @ execute then endof    \ Encoder A Right
    4   of drop abutton @ execute endof               \ Encoder A Button
    8   of 24 and 0= if bleft @ execute then endof    \ Encoder B Left
    16  of 24 and 0= if bright @ execute then endof   \ Encoder B Right
    128 of drop bbutton @ execute endof               \ Encoder B Button
    drop
  endcase
  laststate !                                         \ Push current state to last state
;

: clock-tick-interrupt-handler
  tick @ execute
;

: rtc-interrupt-handler
  0 dup dup rLightLevel ! bLightLevel ! gLightLevel ! \ Lights out
  RTCIV @ drop                                        \ Clear interrupt
  2 RTCCTL bic!                                       \ Disable interrupt
;

: myinit \ ( -- )
  $32 DUP DUP P1DIR cbis! P1SEL0 cbic! P1SEL1 cbis!   \ Port 1, Bits 1,4 & 5 are the blue, green & red channels driven by the timers

  $9F dup dup P2OUT cbis! P2DIR cbic! P2REN cbis!     \ Port 2, Bits - 0, 1, 2, 3, 4, & 7 are for the encoders
                                                      \ 5 & 6 used by uart so don't change
                                                      \ Set these bits as input with internal pullup

                                                      \ Timer A0/A1 for running PWM Lamp / LED dimming duty
  $0008 dup     TA0CTL  bis! TA1CTL bis!              \ Set TACLR to clear timer
  $3E80 dup     TA0CCR0 !    TA1CCR0 !                \ Divide 16MHZ down to 1khz led refresh frequency
  $0000 dup dup TA0CCR1 !    TA1CCR1 !   TA1CCR2 !    \ Lamp initial duty cycle (tick will move this to lightLevel)
  $00E0 dup dup TA0CCTL1 !   TA1CCTL1 !  TA1CCTL2 !
  $0210 dup     TA0CTL !     TA1CTL !                 \ SMCLK/1 Start in up mode

                                                      \ Timer A2 for switch debounce
  $2D0 TA2CTL !                                       \ SMCLK/8 - Up Mode
  1000 d_ticks_per_sec / 2000 * TA2CCR0 !             \ trigger ever ticks_per_sec
  $10  TA2CCTL0 !                                     \ Enable interupts

                                                      \ Timer A3 for updating lamp values
  $110 TA3CTL !                                       \ ACLK/1 - Up Mode
  $7FFF ticks_per_sec /  TA3CCR0 !
  $10  TA3CCTL0 !                                     \ Enable interupts

                                                      \ RTC for timeout
  $3302   RTCCTL !                                    \ VLOCLOCK / 1000 /w interrupts
  timeoutSeconds 10 * RTCMOD !                        \ 10 ticks per second
  $0040   RTCCTL bis!

  ringzero                                            \ Zero ring buffer used by debounce code

                                                      \ Register interrupt handlers and enable interrupts
  ['] debounce-tick-interrupt-handler irq-timerc0 !   \ (C0 is mecrisp's confusing name for A2 main interrupt)
  ['] clock-tick-interrupt-handler irq-timerd0 !      \ (D0 is mecrisp's confusing name for A3 main interrupt)
  ['] rtc-interrupt-handler irq-rtc !                 \ RTC handler

  eint                                                \ Enable interrupts and launch this puppy
;


: init ( -- )                                         \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else myinit then
; 

compiletoram
