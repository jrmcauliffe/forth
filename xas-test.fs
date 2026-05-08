\ Verify the worked examples in docs/cross-assembler-usage.md
\ Prereqs:  compiletoram   #include lib/xas-code.fs

hex

code add1    ( n -- n+1 )
  #1 sp ) add
  next
end-code

code sub1    ( n -- n-1 )
  #1 sp ) sub
  next
end-code

code invert  ( n -- ~n )
  -1 # sp ) bix
  next
end-code

code 2x      ( n -- 2n )
  sp ) sp ) add
  next
end-code
