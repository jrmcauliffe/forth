# Forth vs Assembler debouncer — measured comparison

Same shift-register debounce algorithm, two implementations on
MSP430FR2355 / Mecrisp 2.0.7:

- `db-timer.fs` — `shift-isr` written as a Forth colon definition.
- `db-asm.fs` — `shift-isr` (and `io-init`, `shift-start`,
  `shift-stop`) written in MSP430 assembly via the e4thcom xas
  plug-in, with peripheral names brought in by `\xas MCU:`.

Speed measured by `bench.fs` — read TA1R (free-running SMCLK at
/1, 1 cycle/tick), call `shift-isr` once, read TA1R again,
diff. Dictionary footprint measured by `here` markers around the
implementation in each file.

## Speed

| | Forth | Asm |
|---|---|---|
| Cycles per `shift-isr` call | 200 | 37 |

## Dictionary footprint

| | Forth (`db-timer.fs`) | Asm (`db-asm.fs`, incl. `lib/xas-code.fs`) |
|---|---|---|
| Bytes | 732 | 344 |

## Combined

| | Forth | Asm | Asm / Forth |
|---|---|---|---|
| ISR cycles per call | 200 | 37 | 0.19× |
| Dictionary bytes | 732 | 344 | 0.47× |

Roughly 5× faster, half the size.
