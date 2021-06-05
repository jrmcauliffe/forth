\res MCU: MSP430F5510

#require digital-io.fs
\ SPI Constants
4 0 io CONSTANT RESET
4 1 io CONSTANT MOSI
4 2 io CONSTANT CS
4 3 io CONSTANT SCLK
1 7 io CONSTANT ISDATA

#require ST7735R.fs

2048 Constant MAXITER

\ View of complex plane
0     -1 2Constant IMIN
0      1 2Constant IMAX
$3FFF -2 2Constant RMIN \ -1.75
$BFFF  0 2Constant RMAX \ 1.75

: zmap ( n1 n2 -- df1 df2) \ Map (row, col) to point in complex view z (r, i)
  swap 0 -rot 0 swap       \ convert to fixed point and swap row and col
  RMAX RMIN d- 0 ROWS f/ f* RMIN d+ \ scale rows to real component
  2SWAP
  IMAX IMIN d- 0 COLS f/ f* IMIN d+ \ scal cols to imaginary component
;

: zdup ( z -- z z ) \ duplicate a complex number
  2over 2over
;

: zdrop ( z -- ) \ drop a complex value
  2drop 2drop
;

: z+ ( z1 z2 -- z3 ) \ add two complex numbers
  2rot d+ 2-rot d+ 2swap
;

: zover ( z1 z2 -- z1 z2 z1 ) \ over for complex number
  7 pick 7 pick 7 pick 7 pick
;

: zsquared  ( z1 -- z2 ) \ square a complex number
  2over 2dup f* 2over 2dup f* d- 
  2-rot f* 0 2 f*  
;

: fz ( z1 z2 -- z3 ) \ z c -> z*z + c
  zover                 \ copy z as double doubles are hard to handle with stack ops
  zsquared z+           \ square and add c
  2rot 2drop 2rot 2drop \ drop initial z
;

: z. ( z -- ) \ print a complex number
  2swap 3 f.n ." + " 3 f.n ." i"
;

: escaped? ( z -- flag ) \ is absolute value of z < 2?
  2dup f* 2swap 2dup f* d+ 0 4 d>
;

: mandel? ( z -- flag) \ is this complex numberi c in the mandelbrot set?
  \ zdup          \ save copy of original point
  0 0 0 0       \ Initial z
  0  
  MAXITER 0 do
  drop
  zdup escaped? if i leave then \ if escaped, put iterations on stack
  zover fz       \ run iteration f(z) = z*z + c
  i loop         \ leave i on top of stack
  4 0 do -rot 2drop loop \ Get rid of last z and c values from stack leaving i
;

: scale-colour ( n -- n ) \ scale number of iterations to greyscale shade (assume 565 colour)
  MAXITER 64 /
  * dup 1 rshift tuck \ One for each of RGB (565)
  6 lshift or
  5 lshift or
;

: mandlebrot ( colour -- ) \ scan across each row writing a pixel
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
  key? if else mandlebrot then
;

