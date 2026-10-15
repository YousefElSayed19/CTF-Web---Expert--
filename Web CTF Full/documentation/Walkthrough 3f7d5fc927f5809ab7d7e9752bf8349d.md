# Walkthrough

# Solution Walkthrough

**Challenge:** Task 14 / Challenge #1 — Reverse Engineering (Medium)
**Artifacts:** `game.exe`, `checker.exe`**Tooling used:** Ghidra (static analysis), Python 3 (replicating extracted logic)
**Final flag:** `duck{4yBlGzSTp+5sP4Q3!eG#fE$gHGYjkpQQ}`

---

## 1. Setup

1. Open Ghidra → `File → New Project` → Non-Shared Project.
2. `File → Import File` → import `game.exe`.

![image.png](image.png)

1. When prompted to analyze, accept with default options.

![image.png](image%201.png)

1. Repeat the same import + analyze for `checker.exe` (can be a second file in the same project).

---

## 2. Solving `game.exe`

### 2.1 Program flow (`main()`)

Decompiling `main()` shows the gating order:

```
Password  →  verify_sys_integrity()
           →  run_quiz()
           →  run_mini_game()
           →  reveal_password_2()  (prints "password 2" on success)
```

![image.png](image%202.png)

### 2.2 Recovering Password 1 — `verify_sys_integrity()`

Decompile of `verify_sys_integrity()`:

![image.png](image%203.png)

Key observations:

- `is_debugger_present()` — anti-debug gate. If a debugger is attached, the check always fails silently (same error message either way). This forces pure static analysis rather than step-through debugging.
- The masked string is XOR'd with `1` byte-by-byte. **Only the first 20 bytes are real data** (`NP@QDkm@Ro5l@B[SKvD<`); Ghidra visually concatenates it with an adjacent string literal in `.rdata` — this is a decompiler display artifact, not real program behavior.
- After unmasking, the result is base64 text, which is base64-decoded, then XOR'd with a 4-byte repeating key stored at address `0x140014000`.

**Extracting the XOR key** — go to address `0x140014000` in the Listing view:

![image.png](image%204.png)

```
140014000: 4B 33 79 21        → "K3y!"
```

**Reproducing the decode in Python:**

```python
import base64

masked = "NP@QDkm@Ro5l@B[SKvD<"
unmasked_b64 = ''.join(chr(ord(c) ^ 1) for c in masked)
decoded = base64.b64decode(unmasked_b64)

key = bytes([0x4B, 0x33, 0x79, 0x21])
password1 = bytes(b ^ key[i % 4] for i, b in enumerate(decoded)).decode()

print(password1)  # r3v3rs3_m3_pl2
```

Or can use CyberChef to get password 1 : 

![image.png](image%205.png)

**Result: Password 1 = `r3v3rs3_m3_pl2`**

### 2.3 Quiz

The quiz answers are discoverable directly from the static analysis already performed:

| Question | Answer |
| --- | --- |
| How many distinct XOR key bytes are used? | `4` |
| What are the 4 bytes of the XOR key, in hex? | `4B 33 79 21` |
| What is the name of the function that verifies the password? | `verify_sys_integrity` |

![image.png](image%206.png)

### 2.4 Mini-game

A number-guessing game; the secret number (`7`) is a plain integer literal visible directly in the decompiled `run_mini_game()`.

![image.png](image%207.png)

### 2.5 Password 2

On success, `reveal_password_2()` runs and prints Password 2. It uses the **same hide technique** as Password 1 (repeating-key XOR + base64 + byte-masking) but with its own independent key/mask, so it is statically recoverable the same way if desired — the program printing it at runtime is just the fast path.

![image.png](image%208.png)

**Result: Password 2 = `ch3ck3r_unl0ck_key`**

---

## 3. Solving `checker.exe`

`checker.exe` is a **completely separate binary** — it does not read or depend on `game.exe` at runtime. It requires two independently-verified inputs before it will reveal anything.

### 3.1 Program flow (`main()`)

```
Enter file password   → check_password2()         (SHA-256 hash comparison)
Enter recovered hex   → compute_check_value()      (must be computed, not extracted)
  → if both correct: derive final AES key, decrypt embedded ciphertext, print flag
```

### 3.2 Gate 1 — the file password

`check_password2()` hashes the input with SHA-256 and compares it to a stored hash — the real password string is never stored in the binary. Because this value is *Password 2 from `game.exe`*, you must have solved `game.exe` first.

**Value needed:** `ch3ck3r_unl0ck_key` (already obtained in section 2.5).

![image.png](image%209.png)

### 3.3 Gate 2 — the computed hex value

This is the core of `checker.exe`'s own challenge. Decompiling `compute_check_value()`:

```c
void compute_check_value(longlong param_1)
{
  byte local_9 = 0xa5;
  for (int i = 0; i < 0x10; i++) {
    byte b = CHECK_SEED[i] ^ local_9;
    b = rotate_left(b, (i % 5) + 1);
    b ^= CHECK_CONST[i];
    b = b + i*3 + 7;
    local_9 = b;
    *(byte *)(i + param_1) = b;
  }
}
```

![image.png](image%2010.png)

This is a **chained transform** — each byte's result depends on the previous byte's result (similar in spirit to CBC chaining), so it must be computed in order, not independently per byte.

![image.png](image%2011.png)

![image.png](image%2012.png)

**Extracting the two constant arrays** from the Listing view:

```
CHECK_SEED  @ 0x14001c050:
3f 8a 12 d4 67 e9 0b 55 c2 7a f1 29 84 36 bd 5e

CHECK_CONST @ 0x14001c060:
11 2d 3c 44 58 61 79 82 93 a5 b6 c8 d9 e2 f3 07
```

**Reproducing the transform in Python:**

```python
def rol8(x, n):
    x &= 0xFF
    return ((x << n) | (x >> (8 - n))) & 0xFF

CHECK_SEED  = [0x3f,0x8a,0x12,0xd4,0x67,0xe9,0x0b,0x55,
               0xc2,0x7a,0xf1,0x29,0x84,0x36,0xbd,0x5e]
CHECK_CONST = [0x11,0x2d,0x3c,0x44,0x58,0x61,0x79,0x82,
               0x93,0xa5,0xb6,0xc8,0xd9,0xe2,0xf3,0x07]

prev = 0xA5
out = []
for i in range(16):
    b = CHECK_SEED[i] ^ prev
    b = rol8(b, (i % 5) + 1)
    b ^= CHECK_CONST[i]
    b = (b + (i * 3 + 7)) & 0xFF
    out.append(b)
    prev = b

print(bytes(out).hex())
```

**Result: hex value = `2bb50ef99ea5dcea300e6efd3d8085e4`**

#### 3.4 Deriving the flag (performed by the program after both gates pass)

Once both values are accepted, `checker.exe` internally:

1. Folds the file password into a 16-byte key (`fold_password()` — every character of the password affects the key, not just the first 16 bytes).
2. XORs that 16-byte key with the submitted hex value → final AES-128 key: `6ce67a3f48c33854dad2b69dfb563b32`
3. AES-128-CBC decrypts the embedded `FLAG_CIPHERTEXT` using that key and a fixed IV (`3132333435363738393a3b3c3d3e3f40`).
4. Strips PKCS7 padding and sanity-checks the result looks like printable text ending in `}`.
5. Prints the flag.

This step cannot be bypassed by static reading alone — both the password and the hex value must be correct. However, once both are known, the final decryption can also be reproduced entirely outside the program, confirming the derivation is correct and not dependent on the program’s runtime behavior.

**Manual verification (CyberChef — AES Decrypt):**

- Key (Hex): `6ce67a3f48c33854dad2b69dfb563b32`
- IV (Hex): `3132333435363738393a3b3c3d3e3f40`
- Mode: `CBC`
- Input: `Hex` → `b47a7229f2a7b58aaceb8dcaff6d8a8b43a70c482a9c880929aaddf780ec99ea6e2f90c2315496fd089f5d4b24c5a402`
- Output: `Raw`

Result matches the program’s own output exactly: `duck{4yBlGzSTp+5sP4Q3!eG#fE$gHGYjkpQQ}`

![image.png](image%2013.png)

![image.png](image%2014.png)

---

## 4. Running the full solve

**`game.exe`:**

```
Password: r3v3rs3_m3_pl2
Quiz Q1:  4
Quiz Q2:  4B 33 79 21
Quiz Q3:  verify_sys_integrity
Guess:    7
→ prints password 2: ch3ck3r_unl0ck_key
```

**`checker.exe`:**

```
Enter file password:         ch3ck3r_unl0ck_key
Enter the recovered hex value: 2bb50ef99ea5dcea300e6efd3d8085e4
→ FLAG: duck{4yBlGzSTp+5sP4Q3!eG#fE$gHGYjkpQQ}
```

---