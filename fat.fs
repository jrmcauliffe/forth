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

: sd-cmd ( cmd arg -- u ) \ cmd is double
  rot                     \ switch cmd and arg
  ." CMD: " dup . cr
  -spi 2 us +spi
            $FF >spi
         $40 or >spi
   dup 8 rshift >spi
                >spi
   dup 8 rshift >spi
                >spi
            $95 >spi      \ correct crc-7 byte
  begin $FF >spi> dup $80 and while drop repeat
;

: sd-init ( -- )
  spi-init  spi-slow 200 ms 10 0 do $FF >spi loop
\  0 ticks !
  begin
    0 0 s>d sd-cmd  \ CMD0 go idle
  $01 = until

  begin
    10 ms
    \ 55 0 s>d sd-cmd drop sd-wait
    \ 41 0 s>d sd-cmd
    1 0 s>d sd-cmd
  0= until

  spi-init  \ revert to normal speed

\ 59 0 sd-cmd . sd-wait
\ 8 $1AA sd-cmd . sd-wait
\ 16 $200 sd-cmd . sd-wait
;

: sd-copy ( f n -- )
  swap begin ( dup . ) $FE <> while $FF >spi> repeat
  0 do  $FF >spi> sd.buf i + c!  loop
  $FF dup >spi >spi ;

\ 0 1 2 3 4 5 6 7 8 9 A B C D E F CRC
\ 002E00325B5A83A9FFFFFF80168000916616  Kingston 2GB
\ 007F00325B5A83D3F6DBFF81968000E7772B  SanDisk 2GB

: sd-size ( -- n )  \ return card size in 512-byte blocks
  9 0 sd-cmd  16 sd-copy
\ http://www.avrfreaks.net/forum/how-determine-mmc-card-size
\ https://members.sdcard.org/downloads/pls/simplified_specs/archive/part1_301.pdf
\ TODO bytes 6 and 8 may be reversed...
  sd.buf 6 + c@ $03 and 10 lshift
  sd.buf 7 + c@ 2 lshift or
  sd.buf 8 + c@ 6 rshift or ;

: sd-read ( page -- )  \ read one 512-byte page from sdcard
  9 lshift  17 swap sd-cmd  512 sd-copy ;

: sd-write ( page -- )  \ write one 512-byte page to sdcard
  9 lshift  24 swap sd-cmd drop
  $FF >spi $FE >spi
  512 0 do  sd.buf i + c@ >spi  loop
  $FF dup >spi >spi  sd-wait ;

compiletoram
