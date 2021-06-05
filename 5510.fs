
\res MCU: MSP430F5510
\res export POUT PDIR PREN PDS PSEL0 PSEL1 P4OUT P4DIR P4REN

compiletoflash

#include ms.fs
#include digital-io.fs

\ Onboard green led
4 7 io CONSTANT LED
: led_init OUTMODE-LS LED io-mode! ;
: led_on LED io-1! ;
: led_off LED io-0! ;

\ Onboard lcd
1 CONSTANT LCD_PORT
5 1 io CONSTANT LCD_POWER
LCD_PORT 1 io CONSTANT LCD_RS
LCD_PORT 2  io CONSTANT LCD_RW
LCD_PORT 3 io CONSTANT LCD_E
LCD_PORT 4 io CONSTANT LCD_DB4
LCD_PORT 5 io CONSTANT LCD_DB5
LCD_PORT 6 io CONSTANT LCD_DB6
LCD_PORT 7 io CONSTANT LCD_DB7

\ #require ST7032.fs

