# Objective

This challenge tests whether a participant can:

- Perform **static binary analysis** (Ghidra: import, auto-analyze, navigate the Symbol Tree, read decompiled Pseudo-C, read raw bytes from `.data`/`.rdata`).
- Recognize and reverse common **client-side obfuscation techniques**: multi-byte XOR, Base64 encoding, byte-masking, and chained/sequential bit transforms.
- Identify and bypass a basic **anti-debugging check** without needing to actually debug the binary.
- Trace a **multi-stage gate** (password → quiz → mini-game → second password) across two independent binaries.
- Replicate a **custom algorithm** read from decompiled code in an external tool (Python or CyberChef) to compute a value that is not stored anywhere in the binary.
- Understand how a **symmetric cipher (AES-128-CBC)** is used to protect a secret at rest, and correctly extract the key material, IV, and ciphertext needed to decrypt it.

The challenge is intentionally built so that no step can be skipped by a naive `strings` dump or by guessing — every value must be either extracted correctly or computed correctly, and the two binaries are cryptographically bound together (the flag cannot be obtained from either file alone).