# Switch Debouncing in Firmware — Summary

Summary in my own words of the approach in Jack Ganssle's article
*Debouncing, Part 2* — "Debouncing in firmware".
Source: <https://www.ganssle.com/debouncing-pt2.htm>

This is a working reference; refer to the original for the full
treatment, measurement data, and the author's reasoning.

## The problem

Mechanical switch contacts bounce on closure (and often on release).
Instead of a clean low-to-high or high-to-low edge, the input line shows
a burst of rapid transitions for some bounce period before settling.
Typical bounce windows are a few milliseconds for new switches and tens
of milliseconds for older or grimy ones. Reading the raw line and acting
on every edge will produce phantom presses.

Hardware debouncing (RC filters, dedicated debouncer chips) works but
costs parts. Firmware debouncing trades CPU time for a BOM saving and is
the usual answer in microcontroller projects.

## The general firmware approach

Don't sample the line in the main loop or in an edge-triggered ISR.
Instead, sample it on a regular timer tick and only declare a logical
state change once the input has been stable across several consecutive
samples.

Three things to pick:

- **Tick period** — how often you sample. A few milliseconds (often
  ~5 ms) is a common compromise: long enough that the ISR cost is
  negligible, short enough that response feels instantaneous.
- **Stability count** — how many consecutive matching samples are
  required before you accept the new state. The total debounce latency
  is roughly `tick × count`. ~20–50 ms total is typical for human
  buttons.
- **Where the result lives** — usually a single "logical state" bit per
  switch that the rest of the firmware reads. Main code never touches
  the raw pin.

## Two implementation patterns

Both run inside the timer ISR. They differ in how strict they are about
glitches during the bounce window.

### 1. Counter / integrator

Keep a saturating counter per switch. On each tick, increment if the raw
input reads high, decrement if low; clamp to `[0, MAX]`. The logical
state flips to "pressed" when the counter saturates at `MAX` and back to
"released" when it saturates at `0`.

Properties:

- Tolerant of short glitches mid-bounce — a single spurious sample only
  nudges the counter and is overwhelmed by the surrounding stable
  samples.
- Latency is `MAX × tick` from a clean edge.
- Sub-saturating counter values are an "uncertain" middle ground —
  the logical bit is held at its previous value.

Sketch (illustrative C, written from scratch — not reproduced from the
article):

```c
#define DEBOUNCE_MAX 5
static uint8_t integrator;
static uint8_t output;

void tick_isr(void) {
    if (read_pin()) {
        if (integrator < DEBOUNCE_MAX) integrator++;
    } else {
        if (integrator > 0) integrator--;
    }
    if (integrator == 0)            output = 0;
    else if (integrator >= DEBOUNCE_MAX) output = 1;
    /* else: keep previous output */
}
```

### 2. Shift register / vertical counter

Shift each new raw sample into a small register (typically 8 bits). The
logical state is only updated when the register is all-ones or
all-zeros — i.e. the last N samples were unanimous.

Properties:

- Stricter than the integrator: any single glitch within the window
  resets the wait.
- Easy to implement on small MCUs (one byte per switch, one shift-and-
  mask per tick).
- Very natural to extend to multiple switches in parallel by shifting
  whole bytes/words, with each bit position tracking one input — the
  "vertical counter" trick. With three byte-wide shift slots you can
  count up to 8 ticks of stability for 8 switches at once with a handful
  of bitwise ops per tick.

Sketch:

```c
static uint8_t history;
static uint8_t output;

void tick_isr(void) {
    history = (history << 1) | (read_pin() & 1);
    if (history == 0xFF) output = 1;
    else if (history == 0x00) output = 0;
    /* else: keep previous output */
}
```

## Edge detection

The article also notes the distinction between *level* (is the button
currently held?) and *edge* (did it just transition?). Edges are usually
what UI code wants. The standard pattern is to compare the new logical
state against the previous logical state inside the same ISR (or in a
single read from the main loop) and set a "key just pressed" / "key just
released" event flag.

## Things to watch

- **Don't debounce in the main loop.** Anything that runs at irregular
  intervals can miss bounce or oversample it. Always tick-driven.
- **Pick the tick period before the count.** Too-fast ticks waste CPU;
  too-slow ticks lose responsiveness.
- **Test with real hardware.** Bounce profiles vary wildly between
  switch types and ages. The article includes scope captures motivating
  the chosen numbers.
- **Beware power-on glitches.** Initialise debouncer state from the
  current pin reading rather than always to "released".
- **Matrix keypads** add a wrinkle — you debounce per logical key, not
  per row/column line, and you sample the whole matrix on each tick.

## Picking between the two patterns

| | Integrator | Shift register |
|---|---|---|
| Glitch tolerance during bounce | High | Low |
| Code size | Small (per switch) | Smaller (per switch); scales to many switches via vertical counter |
| Latency from clean edge | `MAX × tick` | `N × tick` (N = register width) |
| Best for | Noisy inputs, single buttons | Many switches at once, clean inputs |

For most projects either works fine; the choice tends to come down to
how many switches you're scanning and how noisy the environment is.
