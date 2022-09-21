compiletoflash

\ I2C0 registers per Section 4.3.17
$40044000 constant I2C_CON
$40044004 constant I2C_TAR
$40044010 constant I2C_DATA_CMD
$40044014 constant I2C_SS_SCL_HCNT
$40044018 constant I2C_SS_SCL_LCNT
$4004406c constant I2C_ENABLE
$40044070 constant I2C_STATUS

\ IO registers per Section 2.19.6.1
$40014080 constant GPIO16_STATUS
$40014084 constant GPIO16_CTRL
$40014088 constant GPIO17_STATUS
$4001408c constant GPIO17_CTRL

\ Pad registers per Section 2.19.6.3
$4001c044 constant GPIO16_PAD_CTRL
$4001c048 constant GPIO17_PAD_CTRL

: initi2c
  \ Setup pin special function i2c0 per Section 2.19.2 - F3
  $3 GPIO16_CTRL !  \ I2C0 SDA
  $3 GPIO17_CTRL !  \ I2C0 SCL

  \ Setup GPIO Pads per Section 4.3.1.3 - PU enabled, Schmitt trigger enabled, slow slew rate
  $6A GPIO16_PAD_CTRL !
  $6A GPIO17_PAD_CTRL !

  \ I2C Initial Configuration per Section 4.3.10.2.1 ( address is set in i2c_cmd )
  $1 I2C_ENABLE bic!     \ disable I2C
  $63 I2C_CON !          \ I2C config (7bit addressing, master mode, standard speed)
  $190 I2C_SS_SCL_HCNT !
  $190 I2C_SS_SCL_LCNT !

  $1 I2C_ENABLE bis!     \ enable I2C
;

\ Display i2c address - https://electricdollarstore.com/dig2.html
$14 constant dig2addr
\ Temperature Sensor i2c address - https://electricdollarstore.com/temp.html
$48 constant tempaddr
$28 constant potaddr
: i2c_cmd
  <builds ( command  -- )  
    , align
  does>   ( data addr -- )
    $1 I2C_ENABLE bic!   \ disable I2C
    swap I2C_TAR !       \ set target address
    $1 I2C_ENABLE bis!   \ enable I2C
    @ I2C_DATA_CMD !     \ send command
    $200 or              \ Set stop bit to final byte send
    I2C_DATA_CMD !       \ Send final byte
;

: gettemp ( addr -- tmp )
    $1 I2C_ENABLE bic!        \ disable I2C
    I2C_TAR !                 \ set target address
    $1 I2C_ENABLE bis!        \ enable I2C
    $00 I2C_DATA_CMD !        \ send register we want to read
 \   $100 I2C_DATA_CMD !
    $300 I2C_DATA_CMD !
     20 ms I2C_DATA_CMD c@    \ read second byte
  \   20 ms I2C_DATA_CMD c@    \ read first byte
  \   swap 8 lshift or 8 rshift
;


: getpot
    $1 I2C_ENABLE bic!        \ disable I2C
    I2C_TAR !                 \ set target address
    $1 I2C_ENABLE bis!        \ enable I2C
    100 I2C_DATA_CMD !        \ send register we want to read
    $300 I2C_DATA_CMD !       \ request read of one byte then stop
    20 ms \ begin $0 I2C_STATUS bit@  until
    I2C_DATA_CMD c@     \ fetch byte
;


$01 i2c_cmd sethex
$02 i2c_cmd setdecimal
$04 i2c_cmd setbrightness


: countdown ( n -- ) \ Countdown from n to 0 on display
  0 swap do i dig2addr setdecimal 1000 ms -1 +loop
;

: pulse
  $FF 0 do i dig2addr setbrightness 10 ms loop      \ 0% to 100%
  0 $FF do i dig2addr setbrightness 10 ms -1 +loop  \ 100% to 0%
  $7F dig2addr setbrightness                        \ 50% brightness
;

: showtemp begin tempaddr gettemp dig2addr setdecimal 500 ms key? until ;

compiletoram

