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
0 -2 2Constant IMIN \ -2.0
0  2 2Constant IMAX \  2.0
0 -3 2Constant RMIN \ -3.0
0  2 2Constant RMAX \  2.0

: zmap ( n1 n2 --  z) \ Map (row, col) to point in complex view z (r, i)
  0 swap IMAX IMIN d- 0 COLS f/ f* IMIN d+ \ scale cols to imaginary component
  rot 
  0 swap RMAX RMIN d- 0 ROWS f/ f* RMIN d+ \ scale rows to real component
  2SWAP
;

: z+ ( z1 z2 -- z3 ) \ add two complex numbers
  2rot d+ 2-rot d+ 2swap
;

: zsquared  ( z1 -- z2 ) \ square a complex number (a + bi)(a + bi) = a*a - b*b + 2abi
  2over 2dup f*  2over 2dup f* d-
  2-rot f* d2*
;

: zover ( z1 z2 -- z1 z2 z1 )
  7 pick 7 pick 7 pick 7 pick
;
 
: zdup 2over 2over ;
: fz ( z1 z2 -- z3 ) \ z c -> z*z + c
  >r >r >r >r        \ push c to return stack
  zsquared 
  r> r> r> r>        \ pop c from return stack
  z+
;
: zdrop 2drop 2drop ;

: z. ( z -- ) \ print a complex number
  2swap 3 f.n ." + " 3 f.n ." i"
;

: escaped? ( z -- flag ) \ is absolute value of z > 2?
  2dup f* 2swap 2dup f* d+ 0 4 d>
;

: mandel? ( z -- n) \ is this complex number in the mandelbrot set?
  \ zdup          \ save copy of original point
  0 0 0 0         \ Initial z
  0         \ dummy value to be dropped below (instead of i after first pass)
  MAXITER 0 do
  drop
  zdup escaped? if i leave then \ if escaped, put iterations on stack
  zover fz       \ run iteration f(z) = z*z + c
  i loop         \ leave i on top of stack
  nip nip nip nip nip nip nip nip \ Get rid of last z and c values from stack leaving i
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
