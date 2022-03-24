\res MCU: MSP430F5510

compiletoflash

#require digital-io.fs
\ SPI Constants
4 0 io CONSTANT RESET
4 1 io CONSTANT MOSI
4 2 io CONSTANT CS
4 3 io CONSTANT SCLK
1 7 io CONSTANT ISDATA

#require ST7735R.fs

256 Constant MAXITER

\ fixpoint 8.8
: >fp 8 lshift ;
: f. dup $FF and 8 lshift swap 8 arshift 3 f.n ;
: f* m* d2/ d2/ d2/ d2/ d2/ d2/ d2/ d2/ drop ;

\ View of complex plane
-2 >fp Constant IMIN \ -2.0
 2 >fp Constant IMAX \  2.0
-3 >fp Constant RMIN \ -3.0
 2 >fp Constant RMAX \  2.0

: zmap ( n1 n2 --  z) \ Map (row, col) to point in complex view z (r, i)
  IMAX IMIN - COLS / * IMIN + \ scale cols to imaginary component
  swap
  RMAX RMIN - ROWS / * RMIN + \ scale rows to real component
  swap
;

: z+ ( z1 z2 -- z3 ) \ add two complex numbers
  rot + -rot + swap
;

: zsquared  ( z1 -- z2 ) \ square a complex number (a + bi)(a + bi) = a*a - b*b + 2abi
  over dup f*      \ a*a
  over dup f*      \ b*b
  -
  -rot f* 1 lshift \ 2ab
;

: zover ( z1 z2 -- z1 z2 z1 )
  2over
;

: zdup 2dup  ;
: fz ( z1 z2 -- z3 ) \ z c -> z*z + c
  2swap
  zsquared
  z+
;
: zdrop 2drop ;
: z. ( z -- ) \ print a complex number
  swap f.  ." + " f. ." i"
;

: escaped? ( z -- flag ) \ is absolute value of z > 2?
  dup f* swap dup f* + 4 >fp >
;

: mandel? ( z -- n) \ is this complex number in the mandelbrot set?
  0 0           \ Initial z
  0                 \ dummy value to be dropped below (instead of i after first pass)
  MAXITER 0 do
    drop
    zdup escaped?   \ If this iteration has escaped
    if i leave then \ put iteration count on stack and leave
    zover fz        \ Run iteration f(z) = z*z + c
    i               \ leave i on top of stack in case final iteration
  loop          
  nip nip nip nip   \ Cleanyup last z and c values from stack leaving i
;

: scale-colour ( n -- n ) \ scale number of iterations to greyscale shade (assume 565 colour)
  MAXITER 64 /
  * dup 1 rshift tuck \ One for each of RGB (565)
  6 lshift or
  5 lshift or
;

: mandelbrot ( colour -- ) \ scan across each row writing a pixel
  lcd-init
  $0000 lcd-colour  \ blank screen
  $00 $00 $00 $7F CASET
  $00 $00 $00 $9F RASET
  RAMWR
  ISDATA io-1!
  CS io-0!

  ROWS 0 do
    COLS 0 do
      \ Grab complex number that represends this row/col
      j i zmap mandel? scale-colour dup 8 rshift >spi >spi
    loop
  loop
  CS io-1!
;

: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else mandelbrot then
;
