compiletoflash
#require digital-io.fs

\ SPI Constants
4 1 io CONSTANT MOSI
4 2 io CONSTANT MISO
4 3 io CONSTANT SCLK
4 6 io CONSTANT CS

#require spi.fs

512 buffer: sd.buf

: sd-wait ( -- )  begin $FF >spi> $FF = until ;

: sd-cmd ( crc cmd arg -- u ) \ cmd is double
  rot             \ switch cmd and arg
  dup ." CMD" . 
  +spi
           $40 or >spi
     dup 8 rshift >spi
                  >spi
     dup 8 rshift >spi
                  >spi
                  >spi      \ crc byte
  100 0 do spi> dup $80 and 0= if leave else drop then loop 
  dup hex. cr
  \ begin spi> dup $80 and while drop repeat \ Wait for R1 response
;

: R3 +spi spi> hex. spi> hex. spi> hex. spi> hex. -spi ;
\ https://electronics.stackexchange.com/questions/602105/how-can-i-initialize-use-sd-cards-with-spi
: sd-init ( -- )
  cr
  spi-init spi-slow     50 ms        \ Init spi and drop speed to 200 khz
  -spi 10 0 do $ff >spi loop        \ DI and CS high for >74 clks to initialise
  \ +spi 2 0 do $ff >spi loop -spi     \ CS low and idle for >16 clks
  -spi
  begin
    $95 0 0 s>d sd-cmd              \ CMD0 to go into SPI mode with CS low
  $01 = until
  $87 8 $01AA $0 sd-cmd  R3 cr         \ CMD8 to check if SDC V2
  $04 and $04 = if
    ." V1 SDC found" cr
  else
    ." V2 SDC found" cr
  then

  $FD 58 $0 $0 sd-cmd drop R3 cr           \ CMD58 read OCR
  101 0 do
    10 ms
    $65 55 0 s>d sd-cmd drop sd-wait      \ Try CMD41 sequence first
    $77 41 $0 $4000 sd-cmd               \ 3.3v
    dup 0= if leave else drop then loop
  0<> if
    101 0 do
      10 ms
      $00 1 0 s>d sd-cmd
      dup 0= if leave else drop then loop
  then 
  \ spi-init                           \ revert to normal speed
  $81 16 512 s>d sd-cmd drop                 \ Set 512k blocks
\ 59 0 sd-cmd . sd-wait
\ 8 $1AA sd-cmd . sd-wait
\ 16 $200 sd-cmd . sd-wait

;

: sd-copy ( f n -- )
  +spi 
  swap begin  dup hex. $FE <> while $FF >spi> repeat
  0 do  $FF >spi> sd.buf i + c!  loop
  $FF dup >spi >spi
  -spi  
;

\ 0 1 2 3 4 5 6 7 8 9 A B C D E F CRC
\ 002E00325B5A83A9FFFFFF80168000916616  Kingston 2GB
\ 007F00325B5A83D3F6DBFF81968000E7772B  SanDisk 2GB

: sd-size ( -- n )  \ return card size in 512-byte blocks
  1 9 0 s>d sd-cmd 16 sd-copy
\ http://www.avrfreaks.net/forum/how-determine-mmc-card-size
\ https://members.sdcard.org/downloads/pls/simplified_specs/archive/part1_301.pdf
\ TODO bytes 6 and 8 may be reversed...
  sd.buf 6 + c@ $03 and 10 lshift
  sd.buf 7 + c@ 2 lshift or
  sd.buf 8 + c@ 6 rshift or
;
: sd-read ( page -- )  \ read one 512-byte page from sdcard
  9 lshift  17 swap sd-cmd  512 sd-copy ;

: sd-write ( page -- )  \ write one 512-byte page to sdcard
  9 lshift  24 swap sd-cmd drop
  $FF >spi $FE >spi
  512 0 do  sd.buf i + c@ >spi  loop
  $FF dup >spi >spi  sd-wait ;

