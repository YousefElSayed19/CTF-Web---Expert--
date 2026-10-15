# Objective

# Objective — VaultCorp Web CTF

**Participant:** yousef elsayed
**Domain:** Web Exploitation
**Difficulty:** Hard
**Flag format:** `duck{...}`

---

## What This Challenge Tests

VaultCorp is a custom-built, multi-stage web exploitation challenge designed to simulate a realistic SaaS application with a chain of security vulnerabilities. Each vulnerability is independently documented in real-world CVEs, OWASP API Security Top 10, and published bug bounty writeups.

The challenge tests the following skills in sequence:

| Stage | Skill |
| --- | --- |
| 1 | JavaScript source analysis + custom encoding (XOR + Base64) |
| 2 | Directory brute-forcing with SecLists |
| 3 | Server-Side Request Forgery (SSRF) with WAF bypass via IP obfuscation |
| 4 | Internal service enumeration via blind SSRF port scanning |
| 5 | Unauthenticated Hangfire dashboard access via loopback trust |
| 6 | Remote Code Execution via Hangfire job injection |

---

## Design Goals

- **Realistic scenario** — every vulnerability reflects a pattern found in real production .NET applications.
- **Chain dependency** — no stage can be skipped. Each step unlocks the next.
- **Per-player isolation** — each registration creates a fully isolated tenant (unique admin credentials, unique flag). No player can interfere with another.
- **No guessing** — every discovery has a logical path. Nothing requires blind luck.

---

## Technology Stack

- **Backend:** ASP.NET Core 8 (Razor Pages)
- **Database:** SQLite (per-tenant isolation via `TenantId` column)
- **Background jobs:** Hangfire (InMemory storage)
- **Deployment:** Docker + docker-compose (single container)

---

## Flag Location

The flag is stored at `/app/flag.txt` inside the container. It is only readable after achieving Remote Code Execution via the Hangfire job injection chain.