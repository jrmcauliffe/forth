# e4thcom 0.8.5.2 — Terminal for Embedded Forth Systems

Copyright (C) 2013–2023 Manfred Mahlow. Distributed under GPL v3+.
Implemented in MINFORTH Plus (a MINFORTH derivative).

## Overview

e4thcom is a terminal program for talking to embedded Forth systems over
serial (or UDP, in 0.5.3 only). It supports 32/64-bit Linux (X86 and
Raspberry/Raspbian) and 64-bit OSX (testing). Default build is Linux X86_64.

In **terminal mode** it forwards keystrokes to the target without local
echo and displays responses. Typing `#` or `\` at the start of a new line
enters **control mode**, where the line is held back and interpreted as a
terminal directive.

## Source Code Uploading

Two directives mirror the standard Forth `include`/`require`:

- `#include <file>` — unconditional upload (abbrev `#i`)
- `#require <file>` — uploads only if a target word equal to the filename
  isn't already in the dictionary (abbrev `#r`)

After upload, if no target word with the file's name exists, a NOOP word
of that name is created — so future `#require`s are no-ops.

File look-up path: `cwd:cwd/mcu:cwd/target:cwd/lib` (overridable with
`-p`). Convention:
- `cwd` — project code
- `cwd/mcu` — hardware-specific code
- `cwd/target` — target-specific code
- `cwd/lib` — target-independent code

Uploads are line-by-line; the terminal waits for the target's prompt
before sending the next line. Comments starting with `\` (or inside `{ }`)
are displayed but not uploaded. Press **TAB** to cancel an upload (use
`[ Enter` afterwards if the target was left in compile mode).

### Conditional terminal directives (since 0.8.5)

- `#ifdef <name> ...` — runs the rest of the line if `<name>` is in the
  target's dictionary.
- `#ifndef <name> ...` — runs the rest of the line if `<name>` is not.

The trailing fragment can be `#include`, `#require`, `\res ...`, `\\`,
or Forth code.

## Data Transmission

Default is **full-duplex**. Use `--hdm` for half-duplex if the channel
can't keep up (the option was `--half-duplex` before 0.8.0).

Supported baud rates: B1200, B2400, B4800, B9600, B19200, B38400, B57600,
B115200, **B230400, B460800, B500000, B576000, B921600, B1000000**
(rates above B115200 added in 0.8.4).

## Command Line

Input is buffered until **Enter**. To send a single character with no CR,
prefix with `\` in an empty line, e.g. `\x [Enter]`. Toggle buffering with
`\^ [Enter]`.

Two buffer modes:
- **Default** — internal editor (since 0.5.4).
- **Readline** — enable with `--rln` (since 0.8.2).

Editing keys: `[Backspace] [Left] [Right] [Home] [End] [UP] [Down]`.
`[Shift]+[TAB]` clears the line. TAB cycles history matches.

Extend history from source with `\index phrase1  phrase2  ...  phraseN`
(two spaces between phrases; directive must be first on the line).

### Editor integration

- `#edit <file>` opens a file in your configured editor.
- After an upload error, `#edit` (or `#e`) opens the failed file at the
  offending line.
- `#ls [OPTION]... [FILE]...` shells out to `ls`.
- Editor command lives in `.e4thcom-x.y.z` (search order
  `cwd:cwd/dir1:e4thcom-dir`).

## Installing

Distributed as a tar.gz; unpack anywhere. For system-wide install, unpack
into `/opt`, `chown -R root:root /opt/e4thcom-x.y.z`, then symlink
`/usr/local/bin/e4thcom` → the binary.

User must have access to serial devices — typically membership in
`plugdev`, `dialout`, and `dip`. Check with `cat /etc/group | grep <user>`,
add with `sudo adduser <user> <group>`.

Filename limits: absolute paths max 128 chars; relative paths max 32
chars (also the max Forth word-name size).

## Starting e4thcom

```
/path/to/e4thcom-x.y.z/e4thcom [options]
```

| Option | Meaning |
|--------|---------|
| `-t <target>` | Load `<target>.efc` plug-in |
| `-p <dir1:dir2:dir3>` | Source look-up path |
| `-d <device>` | Serial device (`ttyS0`, `ttyUSB0`, `ttyACM0`, …) or `ip:port` (0.5.3 UDP only) |
| `-b <baud>` | Baud rate |
| `-h` | Help text |
| `--hdm` | Half-duplex |
| `--idm` | Inverted display (black/coloured on white) |
| `--rln` | Readline command-line buffering |

Close with `\ [Enter]` (Esc in pre-0.5.4).

## Plug-Ins

Since 0.4 there's a single binary plus per-target `*.efc` Forth plug-ins
that handle each target's prompt/error idioms. Shipped plug-ins:

- `anyforth.efc` — dumb uploader, no error checking, only `#include`
- `380eforth.efc` / `-xas` — 328eForth (Arduino Uno, C.-H. Ting), +ATmega xas
- `430camelforth.efc` / `-xas` — 430CamelForth (+MSP430 xas/dis)
- `430eforth.efc` / `-xas` — 430eForth
- `4e4th.efc` / `-xas` — 4e4th
- `amforth.efc` / `-xas`
- `mecrisp.efc` — Mecrisp; `mecrisp-msp430xas.efc` adds MSP430 xas/dis
- `mecrisp-qs.efc` — Mecrisp-Quintus (since 0.8.4)
- `mecrisp-st.efc` — Mecrisp-Stellaris
- `noforth.efc` / `-xas`
- `stm8ef.efc` — stm8eForth (since 0.6.2)
- `swapforth-j1sc.efc` — swapforth (since 0.8.2)

Per-plug-in handshake highlights:
- **noForth** — uses `^ACK`/`^NAK` control chars; the most reliable.
- **mecrisp / -Stellaris / -Quintus** — combination of timeout + text
  analysis (`' ok.^NL'` = ready, timeout after `'message^NL'` = error).
- **amforth** — looks for `' ok'` / `' ^CR^NL'`.
- **4e4th** — XON/CR/NL pattern; error messages from `abort"` must end
  with `?`.
- **stm8ef** — uses `' ok^NL'` / `' OK^NL'` / `'^BEL^NL'`.
- **430CamelForth / 430eForth** — timeout + text analysis; ambiguous if
  the target waits for input mid-upload.

## Cross Assembling

A target plug-in may bundle a Forth-written cross assembler. When loaded,
these become terminal directives:

- `code <name> ( -- ) ... end-code` — `code` is sent to the target,
  subsequent lines are assembled locally and comma-compiled into the
  target until `end-code`. Example:
  ```forth
  code led1.on ( -- )
      BIT0 # P1OUT & .b bis next end-code
  ```
- `label LB[01..05]` — predefined globals; assigns the next code address.
- `\xas` — line is interpreted by the cross-assembler's internal Forth
  rather than uploaded. Used for macros or to load `.efr` files
  (`\xas MCU: name`).
- `x[ ... ]` — temporarily disable the assembler, send text to the target,
  read back one cell. Lets cross-assembled code reference values defined
  on the target:
  ```forth
  code base@ ( -- u )
      x[ base ] & push, next end-code
  ```

**Number base in cross-assembler mode is hex.** The assembler is case-
sensitive; the author's convention is lowercase operators, uppercase
register/bit identifiers.

## Cross Disassembling

If the plug-in includes a disassembler, `#das` (abbr. `#d`) is enabled:

- `#das <name>` — disassemble starting at the xt of `<name>`.
- `#das` — pop an address from the target's TOS and disassemble there.

## Resource Files

Modern MCUs need lots of register/bit constants. The `\res` directive
manages these in the terminal's own dictionary:

- `\res <number> equ <name>` — define/update a constant in the
  *terminal's* dictionary.
- `\res export name1 ... nameN` — push these constants to the target as
  constants, if not already defined there.
- `\res MCU: <name>` — load `<name>.efr` (search order
  `cwd:dir1:e4thcom-dir`; `dir1` defaults to `mcu`).

## Open Issues / Notes

- No device locking — another process using the same serial device can
  steal characters.
- More targets and cross assemblers welcome via the plug-in mechanism.
- ForthBox GUI is no longer bundled (depends on 32-bit libvte).

Last revision: MM-230429.
