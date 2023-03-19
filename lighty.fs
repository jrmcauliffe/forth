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

\ 16MHZ
: us 0 ?do [ $3C00 , $3C00 , $3C00 , $3C00 , $3C00 , $3C00 , ] loop ;
: ms 0 ?do 998 us loop ;

$0000           variable laststate
200             constant d_ticks_per_sec              \ Number of debounce ticks in a second
64              constant ticks_per_sec                \ Number of clock ticks in a second
20              constant origLightLevel               \ Default light level on power on / resume from sleep
600             constant timeoutSeconds               \ Timeout to off
TA1CCR1         constant rTimer                       \ Red CCR register
TA1CCR2         constant gTimer                       \ Green CCR register
TA0CCR1         constant bTimer                       \ Blue CCR register

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
                                                      \ Variable with function to update
: v ( default xt -- ) swap 2 nvariable ;              \ Allocate space for var and handler

: v' ( n v -- )                                       \ Execute variable handler and writeback to variable
  tuck                                                \ Save variable address for final write
  dup @                                               \ Get current variable value
  swap cell+ @                                        \ Get execution handler
  execute swap !                                      \ Execute add write back to var
;

: group ( v1 v2 vn n -- vg )                          \ Set of variables for a function
  <builds dup , 0 do , loop                           \ Allocate space for count and then each variable on build
  does>                                               \ Return address of variable group on call
;

: selected ( v g - n )                                \ Get the object in the group represented by the var
  cell+ swap                                          \ Get address of group and skip size
  @ cells + @                                         \ Add index of stored in variable and return
;

: f ( xt vg -- r g b )                                \ Function to take var group and an xt return an rgb value
  <builds
  , ,                                                 \ Save var group and function xt
  does>                                               \ Return address of var group
;

: f' ( f -- r g b )
  dup cell+ @ swap                                    \ grab the xt and save on stack for the end
  @ dup                                               \ memory address of vgroup with copy
  @                                                   \ count of vars in group
  swap cell+ dup rot cells +                          \ Start and end of memory address for vars
  swap do i @ @ swap 1 cells +loop                    \ Grab all the variable values keeping xt address TOS
  execute                                             \ Run the function
;

: clamp 90 min 0 max ;                                \ Fix value between max and min values

: reset-rtc $0042 RTCCTL bis! ;                       \ Restart countdown timer

: nopFunc drop drop 0 ;                               \ Do nothing variable handler

: rotary ( isLeft currentVal -- newVal )              \ Rotary encoder variable handler
  swap if 2- else 2+ then clamp                       \ Boolean input to determine direction, add or subtract then clamp
;

: button ( newVal currentVal -- newVal ) drop ;       \ Button variable handler
: modCount ( mod currentVal -- newval ) 1+ swap mod ; \ Looping counter variable handler

: passThru ( n n n -- n n n) ;
: fanOut ( n --- n n n ) dup dup ;

                                                      \ RGB Scheme
20 ' rotary v vRLevel                                 \ Variables to manually set R, G and B values
20 ' rotary v vGLevel
20 ' rotary v vBLevel

vGLevel vBLevel vRLevel 3 group vgRGB
' passThru vgRGB f fRGB                               \ Directly pass through these values

                                                      \ White Light Scheme
20 ' rotary v vWLevel                                 \ Single value for light level
vWLevel 1 group vgWhite
' fanOut vgWhite f fWhite                             \ Simple fanout function to copy same value to RGB



fRGB fWhite 2 group myfunctions                       \ List of all functions to cycle through

0  ' modCount v vVarIndex
20 ' rotary   v vGlobal                               \ Global Illumination
0  ' modCount v vFuncIndex




: fCurr vFuncIndex myfunctions selected ;             \ Return currently selected function
: vCurr vVarIndex fCurr @ selected ;                  \ Return currently selected variable

: closeChannel ( lvl timer -- )                       \ Close in on desired value to avoid abrupt light level changes
  dup rot                                             \ Save a copy of the timer CCR address for later
  vGlobal @ * 1 lshift swap @                         \ Calculate the desired CCR by scaling by global light level
  dup -rot - 3 arshift                                \ Find the difference and then divide this by 2^3 (8)
  dup 0= if drop                                      \ if 0 then we're close enough to assume the desired value
  else + then swap !                                  \ otherwise add offset to close in on desired CCR value
;

: closeIn ( -- )
  fCurr f'                                            \ Run function against variables for RGB levels
  rTimer closeChannel                                 \ Close in on light level for each channel
  gTimer closeChannel
  bTimer closeChannel
;

' closein     variable tick                           \ Default function called by tick handler

: debounce-tick-interrupt-handler                     \ See http://www.ganssle.com/debouncing.htm
  P2IN c@ not $9F and >ring                           \ Read input port skipping uart pins (5 & 6), invert and write to buffer
  ringAnd laststate @ 2dup not and                    \ Check what's changed from 0->1, copy previous state for rotary encoders
  dup 0<> if reset-rtc then                           \ Any change reset timeout
  case                                                \ Call correct function for each input if triggered
    1   of 3 and 0= if true vGlobal v' then endof     \ Encoder A Left   - Decrese global light value
    2   of 3 and 0= if false vGlobal v' then endof    \ Encoder A Right  - Increase global light value
    4   of drop 0 vVarIndex !                         \ Encoder A Button - Reset Selected Var
           myFunctions @ vFuncIndex v' endof          \                    Cycle to next function functions
    8   of 24 and 0= if true vCurr v' then endof      \ Encoder B Left   - Decrease currently selected var
    16  of 24 and 0= if false vCurr v' then endof     \ Encoder B Right  - Increase currently selected var
    128 of drop fCurr @ @ vVarIndex v' endof          \ Encoder B Button - Cycle to next variable in group for current function
    drop
  endcase
  laststate !                                         \ Push current state to last state
;

: clock-tick-interrupt-handler
  tick @ execute
;

: rtc-interrupt-handler
  0 vGlobal !
  origLightLevel vWLevel !                            \ Set the scheme back to white light for restart
  0 vFuncIndex !
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
  $290 TA2CTL !                                       \ SMCLK/4 - Up Mode
  1000 d_ticks_per_sec / 1000 * TA2CCR0 !             \ trigger ever ticks_per_sec
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
