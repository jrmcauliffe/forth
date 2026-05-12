# MSP430 Cross-Assembler — Basic Usage

The e4thcom MSP430 cross-assembler (`-t mecrisp-msp430xas`) lets you write
inline assembly inside `code` … `end-code` blocks. The host assembles each
line and streams the resulting machine code to the target, where it's
appended to a freshly-created Mecrisp word.

This document covers the everyday syntax. The authoritative reference is
the assembler source: `/home/jon/bin/e4thcom-0.8.5.2/msp430xas.efx`.

## Setup

Already in place in this project:

- Launcher (`forth-connect`) starts e4thcom with `-t mecrisp-msp430xas`,
  which loads `mecrisp.efc` first, then the xas core, then the
  disassembler.
- `lib/xas-code.fs` defines the `code` and `end-code` words on the target.
  `#include` it once after a fresh boot (or after `rewind-to-basis`):

  ```forth
  compiletoram
  #include lib/xas-code.fs
  ```

## The big gotcha — file upload only, multi-line form

Two separate gating rules:

1. The cross-assembler only intercepts `code` … `end-code` blocks during
   `#include` of a `.fs` file. Typed at the interactive prompt, the
   assembler does not engage — `code <name>` runs the plain Forth word from
   `lib/xas-code.fs`, and any mnemonics you type after it (e.g. `next`)
   are sent verbatim to the target as Forth and rejected as undefined.

2. **`code <name>` must be on its own line, with the assembly on the lines
   that follow.** Putting the body on the same line as `code <name>` (e.g.
   `code add1 #1 sp ) add next end-code`) does not engage the xas — the
   tail is sent to the target as Forth.

The shape that works:

```forth
code add1
  #1 sp ) add
  next
end-code
```

## Syntax basics

Assembly source is **RPN**: operands first, opcode last. A typical
two-operand instruction is `<source> <destination> <opcode>`.

```forth
code add1            ( n -- n+1 )
  #1 sp ) add        \ ADD #1, 0(R4)
  next               \ RET
end-code
```

### Addressing modes

These are postfix markers on a register or value:

| Form | Means | Example |
|------|-------|---------|
| `Rn` | register direct | `r10` |
| `Rn )` | register indirect (`@Rn`) | `sp )` |
| `Rn )+` | autoincrement indirect (`@Rn+`) | `sp )+` |
| `<n> Rn x)` | indexed (`n(Rn)`) | `2 sp x)` |
| `Rn -)` | predecrement (synthesised) | `sp -)` |
| `<n> #` | immediate (`#n`) | `100 #` |
| `<addr> &` | absolute | `P1OUT &` |
| `.b` | byte-mode flag (suffix on the instruction) | `.b mov` |

Note: MSP430 hardware has no register-indirect *destination* mode. The
assembler quietly rewrites `Rn )` as a destination into `0 Rn x)`
(see `msp430xas.efx:92`), so you can write the natural form regardless.

### Common shorthands

The assembler pre-defines convenient immediates and absolute mode:

```
#0  #1  #2  #4  #8  #-1     \ small constants via the constant generator
&                            \ absolute mode (= sr x))
```

So `#1 sp ) add` and `1 # sp ) add` both work; the first is shorter and
uses MSP430's constant generator (no extension word).

### Byte vs. word

Default is 16-bit word. Append `.b` after operands and before the opcode
for byte mode:

```forth
BIT0 # P1OUT & .b bis    \ BIS.B #BIT0, &P1OUT  — set P1.0 high
```

## Mecrisp/MSP430 register conventions

The plug-in defines these names (see `mecrisp-msp430xas.efc:31-39`):

| Name | Hardware | Role |
|------|----------|------|
| `pc` | R0 | program counter |
| `rp` | R1 | **return** stack pointer (= MSP430 hardware SP) |
| `sr` | R2 | status register |
| `cg` | R3 | constant generator |
| `sp` | R4 | **data** stack pointer |
| `index` | R5 | inner loop index |
| `limit` | R6 | inner loop limit |
| `w` (alias `r07`) | R7 | scratch — preserved across IRQs only |
| `r08`–`r15` | R8–R15 | general (R8/R9 unused by Mecrisp; R10–R15 always saved) |

Two practical implications:

- **Top of data stack lives at `(sp)` (= `0(R4)`).** To modify TOS in
  place, write `… sp ) <op>` (auto-rewritten to `0 sp x)`).
- **`rp` is the hardware SP**, used by `CALL`/`RET`. Don't confuse it
  with `sp`.

## Instruction set

Mnemonics are lowercase. Names that collide with stock Forth words have a
`>` suffix; the assembler also defines collision-free aliases (`bix`/`bia`).

### Two-operand: `<src> <dst> <op>`

Flags follow MSP430 hardware semantics (operation is `dst <op>= src`).

| Mnemonic | Opcode | Effect |
|----------|--------|--------|
| `mov` | 4000 | `dst := src` |
| `add` | 5000 | `dst := dst + src` |
| `addc` | 6000 | `dst := dst + src + C` |
| `sub` | 8000 | `dst := dst - src` |
| `subc` | 7000 | `dst := dst - src - 1 + C` |
| `cmp` | 9000 | flags from `dst - src` (no writeback) |
| `dadd` | A000 | BCD `dst := dst + src + C` |
| `bit` | B000 | flags from `dst AND src` (no writeback) |
| `bic` | C000 | `dst := dst AND NOT src` (clear bits) |
| `bis` | D000 | `dst := dst OR src` (set bits) |
| `xor>` / `bix` | E000 | `dst := dst XOR src` |
| `and>` / `bia` | F000 | `dst := dst AND src` |

Append `.b` to the operands (before the mnemonic) for byte mode:
`BIT0 # P1OUT & .b bis`.

### One-operand: `<dst> <op>`

| Mnemonic | Opcode | Effect |
|----------|--------|--------|
| `rrc` | 1000 | rotate right through carry |
| `rra` | 1100 | arithmetic shift right |
| `swpb` | 1080 | swap high/low bytes |
| `sxt` | 1180 | sign-extend low byte to word |
| `push` | 1200 | push to return stack (`rp`) |
| `call` | 1280 | call subroutine |

### Standalone words

| Word | Emits | Notes |
|------|-------|-------|
| `ret` | 4130 | `mov @rp+, pc` |
| `reti` | 1300 | return from interrupt |
| `next` | 4130 | mecrisp alias for `ret` |

### Status-register macros

| Word | Equivalent |
|------|------------|
| `setc` / `clrc` | `#1 sr bis` / `#1 sr bic` |
| `eint` / `dint` | `#8 sr bis` / `#8 sr bic` |

### Structured control flow

The xas provides Forth-style structured assembler — push a condition, then
use the matching closing word. All compose; closes match opens like
brackets.

```
<cond> if,    body    then,
<cond> if,    body    else,    body    then,
       begin,            <cond> until,
       begin,    body    again,
       begin,    body    <cond> while,    body    repeat,
       ahead,                      then,
<addr> jmp                                          \ relative if in range, else absolute
```

### Conditional flags

Place after a `cmp`/`bit`/arithmetic op, before `if,`/`until,`/`while,`.

| Cond | Opcode | Body runs when… |
|------|--------|-----------------|
| `=?` / `0=?` | 2000 | Z = 1 (equal / zero) |
| `<>?` / `0<>?` | 2400 | Z = 0 (not equal / non-zero) |
| `cs?` | 2800 | C = 1 (carry set) |
| `cc?` | 2C00 | C = 0 (carry clear) |
| `pos?` | 3000 | N = 0 (non-negative) |
| `>?` | 3400 | signed `src > dst` |
| `<eq?` | 3800 | signed `src ≤ dst` |
| `u>?` | 2C00 | unsigned `src > dst` |
| `u<eq?` | 2800 | unsigned `src ≤ dst` |
| `never` | 3C00 | never (used internally by `again,`/`ahead,`) |

After `<a> <b> cmp`, the order-test names read left-to-right: `>?`
means "if `a` greater than `b`". `cs?`/`cc?` and `u>?`/`u<eq?` share
opcodes — pick the name that matches the context (flag check vs.
unsigned comparison) for readability.

## Examples

All of these are valid bodies of `code <name> … end-code` blocks. Each
ends in `next`, which the xas turns into `ret` (0x4130).

### Increment / decrement TOS

```forth
code add1   ( n -- n+1 )
  #1 sp ) add
  next
end-code

code sub1   ( n -- n-1 )
  #1 sp ) sub
  next
end-code
```

### Bitwise NOT of TOS

```forth
code invert ( n -- ~n )
  -1 # sp ) bix
  next
end-code
```

The xas defines two mnemonics for the XOR opcode (E000): `xor>` and
`bix`. **Bare `xor` is not defined**, and `xor>` trips an `xeval`
parser issue at upload time — use `bix`. Emulated instructions like the
standard MSP430 `INV` aren't pre-defined either; the underlying
`BIX #-1, dst` (= `XOR #-1, dst`) is what you write.

Note also that the constant-generator shorthand `#-1` doesn't combine
cleanly with this opcode in our test (`end-code: unbalanced stack`).
Use the long form `-1 #` (literal then immediate-mode marker) instead.

### Double TOS (shift left by 1)

```forth
code 2x     ( n -- 2n )
  sp ) sp ) add
  next
end-code
```

ADD with the same source and destination is the standard MSP430 idiom for
left-shift by one (the carry flag captures the high bit). Avoid `2*` as a
word name — Mecrisp already defines it.

### Set a GPIO bit

```forth
\res MCU: MSP430FR2355                \ pulls in BIT0, P1OUT, etc. — see note
code led-on ( -- )
  BIT0 # P1OUT & .b bis
  next
end-code
```

> Note: loading peripheral names from a `.efr` file via `\res` / `\xas`
> needs a working directive path — at the time of writing the prompt
> directive `\res MCU: …` failed in this build; loading from inside a
> `.fs` file with `#include` of an appropriate definitions file is the
> reliable route. Investigate when you first need peripheral mnemonics.

## Verification

After `#include`-ing a file with `code` blocks, run the words at the
prompt and check the data stack with `.s`. To inspect the assembled bytes:

```forth
#d <name>
```

The disassembler starts at the word's xt and prints instructions
indefinitely (no length marker), so look only at the leading lines that
match what you wrote.

## Notes on the `code` / `end-code` glue

`lib/xas-code.fs` builds `code`/`end-code` out of stock `:` and `;`,
which means every code-word ends with **two** `ret`s — one from your
`next`, one auto-emitted by `;`. The trailing one is unreachable, so the
overhead is a fixed 2 bytes per code-word regardless of body size.
You can drop `next` and let `;`'s implicit `ret` close the word, but the
explicit `next` is conventional and makes multi-exit code clearer.

## Where to look next

- `msp430xas.efx` — full instruction set, control structures (`if,`,
  `until,`, `while,`, etc.), `label LB01`–`LB05`.
- `mecrisp-msp430xas.efc` — register names, the `next` macro, and the
  `x!` flash-aware writeback used by the assembler.
- TI MSP430 Family User's Guide — instruction encoding and addressing
  mode reference.
