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

\ View of complex plane
-2000 Constant IMIN \ -2.000
 2000 Constant IMAX \  2.000
-3000 Constant RMIN \ -3.000
 2000 Constant RMAX \  2.000

: zmap ( n1 n2 --  n1 n2) \ Map (row, col) to point in complex view z (r, i)
  IMAX IMIN - COLS / * IMIN + \ scale cols to imaginary component
  SWAP
  RMAX RMIN - ROWS / * RMIN + \ scale rows to real component
  SWAP
;

: z+ ( z1 z2 -- z3 ) \ add two complex numbers
  rot + -rot + swap
;

: f* 1000 */ ;


: zsquared  ( z1 -- z2 ) \ square a complex number (a + bi)(a + bi) = a*a - b*b + 2abi
  over dup f*  over dup f* -
  -rot f* 2000 f*
;

: fz ( z1 z2 -- z3 ) \ z c -> z*z + c
  2over              \ copy z as double doubles are hard to handle with stack ops
  zsquared z+        \ square and add c
  rot drop rot drop  \ drop initial z
;

: z. ( z -- ) \ print a complex number
  swap . ." + " . ." i"
;

: escaped? ( z -- flag ) \ is absolute value of z > 2?
  dup f* swap dup f* + 4000 >
;

: mandel? ( z -- n) \ is this complex number in the mandelbrot set?
  \ zdup          \ save copy of original point
  0 0       \ Initial z
  0         \ dummy value to be dropped below (instead of i after first pass)
  MAXITER 0 do
  drop
  over over escaped? if i leave then \ if escaped, put iterations on stack
  2over fz       \ run iteration f(z) = z*z + c
  i loop         \ leave i on top of stack
  nip nip nip nip \ Get rid of last z and c values from stack leaving i
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
  ROWS 0 do
    COLS 0 do
      \ Grab complex number that represends this row/col
        j i zmap mandel? scale-colour j i setpixel
    loop
  loop
;

: init ( -- ) \ Launch program if no keypress after 3 sec
  ." Press <enter> for console"
  10 0 do ." ." 300 ms key? if leave then loop
  key? if else mandelbrot then
;

