# Ganssle Debouncer: Forth vs Assembly

Benchmark of `db-ganssle.fs` (Forth) and `db-ganssle-asm.fs` (asm + R08
register-resident state) running on MSP430FR2355 / Mecrisp Forth.

Cycles measured with TA1 free-running at SMCLK/1 (1 cycle per tick).
`shift-isr cycles (alone)` = `bench` − `empty-bench`, factoring out the
two `TA1R @` reads and `swap -` of the harness.

| | Forth | Asm |
|---|---|---|
| **dict-start** | 8496 | 8496 |
| **dict-end** | 8936 | 8752 |
| **Total growth** | **440 bytes** | **256 bytes** |
| **xas-code.fs glue** | — | 40 bytes |
| **Debouncer code only** | **440 bytes** | **216 bytes** |
| | | |
| `empty-bench` | 6 | 6 |
| `bench` | 75 | 28 |
| `shift-isr` cycles (alone) | **69** | **22** |

So the asm version is **~3.1× faster** (69 → 22 cycles) **and ~2× smaller**
(440 → 216 bytes). Both wins from the same source: each Forth-level
operation has to leave the data stack in a defined state, which costs
cycles *and* bytes per token. Asm just keeps the value in R08 and emits
exactly the instructions it needs.

The 40-byte `lib/xas-code.fs` overhead is a one-time fixed cost — it pays
for itself the moment you have more than one `code` block — so as the asm
surface grows, the relative size win grows with it.
