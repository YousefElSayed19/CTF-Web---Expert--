# Reverse Engineering CTF — Task 14 / Challenge #1

**Participant:** yousef elsayed
**Domain:** Reverse Engineering
**Difficulty:** Medium
**flag:** duck{...}

## What this is

A custom-built, two-binary reverse engineering crackme (`game.exe` + `checker.exe`), designed and built from scratch for this task, then solved end-to-end to validate it and produce the evidence below.

## Repo layout

```
.
│
├── bin/                        ← the compiled, playable challenge files
│   ├── game.exe
│   └── checker.exe
│
├── documentation/              ← the written evidence/report (exported from Notion)
│   ├── Objective ....md
│   ├── Public Briefing ....md
│   ├── Walkthrough ....md
│   ├── What Happened and Why (Root Cause) ....md
│   ├── Mitigation ....md
│   └── image*.png              ← screenshots referenced from Walkthrough.md
│
└── src/                         ← full source code (private repo only — never ship to participants)
    ├── BUILD_INSTRUCTIONS.md   ← how to compile game.exe / checker.exe from source
    ├── game.c
    ├── checker.c
    ├── aes.c / aes.h
    ├── base64.c / base64.h
    ├── sha256.c / sha256.h
    ├── test_sha256.c            ← dev-only: validates the SHA-256 implementation
    └── gen_checker_v3.c         ← dev-only: generates the embedded constants in checker.c
```

## Where to start

| If you want to... | Go to |
|---|---|
| Understand what the challenge is testing | `documentation/Objective ....md` |
| See the player-facing description (no spoilers) | `documentation/Public Briefing ....md` |
| Reproduce the full solve, step by step | `documentation/Walkthrough ....md` |
| Understand the security root cause being demonstrated | `documentation/What Happened and Why (Root Cause) ....md` |
| See mitigation recommendations for real-world systems | `documentation/Mitigation ....md` |
| Rebuild the binaries from source | `BUILD_INSTRUCTIONS_V3.md` |

## Design summary

- `game.exe` and `checker.exe` are independent binaries — `checker.exe` never reads anything from `game.exe` at runtime.
- `game.exe`: recover an obfuscated password (multi-byte XOR + Base64 + byte-masking) → pass a knowledge quiz about that analysis → win a mini-game → receive a second, independently-obfuscated password.
- `checker.exe`: requires (1) that second password, verified only via a stored SHA-256 hash, and (2) a 16-byte value that is **not stored anywhere in the binary** — it must be computed by reading and replicating a custom chained transform found in the decompiled code. Only when both are correct does the program derive the final AES-128 key, decrypt the embedded flag, and print it.
- Neither binary leaks the flag, the flag's format, or either password to a plain `strings` scan.

## Evidence status

- [x] Challenge designed and built (C, compiled with MinGW/TDM-GCC)
- [x] Full solve performed and verified (Ghidra static analysis + Python/CyberChef reproduction)
- [x] Flag confirmed reproducible via two independent paths (running `checker.exe`, and manual offline decryption)
- [x] `strings` scan confirms no leakage of flag/passwords on either binary
- [x] Full Walkthrough, Objective, Public Briefing, Root Cause and Mitigation documentation completed
