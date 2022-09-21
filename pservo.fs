compiletoflash

\ IO registers per Section 2.19.6.1
$400140A8 constant GPIO21_STATUS
$400140AC constant GPIO21_CTRL

\ Pad registers per Section 2.19.6.3
$4001c058 constant GPIO21_PAD_CTRL

\ PWM registers per Section 4.5.2
$40050028 constant CH2_CSR
$4005002c constant CH2_DIV
$40050034 constant CH2_CC
$40050038 constant CH2_TOP


: initpwm
  \ Setup pin special function pwm per Section 2.19.2 - F3
  $4 GPIO21_CTRL !  \ PWM2 B

  \ Setup GPIO Pads per Section 4.3.1.3 - PU enabled, Schmitt trigger enabled, slow slew rate
  $6A GPIO21_PAD_CTRL !

  \ Configure PWM registers per Section 4.5.3
  \ Set Integer component of divider to 40
  40 4 lshift CH2_DIV !

  \ Set Top to 62500
  62500 CH2_TOP ! 

  \ Set CC to 4688 (7.5 % duty cycle - centered)
  4688 CH2_CC !

  \ Enable PWM
  $0001 CH2_CSR !
  
;




compiletoram
