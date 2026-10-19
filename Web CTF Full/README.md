# README

# VaultCorp — Web Exploitation CTF

**Participant:** yousef elsayed
**Domain:** Web Exploitation
**Difficulty:** Hard
**Flag format:** `duck{...}`

---

## What This Is

A custom-built, multi-stage web exploitation challenge simulating a realistic SaaS application built with ASP.NET Core. The challenge chains six real-world vulnerabilities — each independently documented in CVEs and bug bounty reports — into a single exploit path from anonymous visitor to Remote Code Execution.

---

## Repo Layout

```
.
│
├── bin/                        ← challenge files for the participant
│   └── docker-compose.yml      ← everything needed to run the challenge
│
├── documentation/              ← written evidence / report
│   ├── Objective.md
│   ├── Public Briefing.md
│   ├── Walkthrough.md
│   ├── What Happened and Why (Root Cause).md
│   └── Mitigation.md
└── src/                        ← full source code (private — never ship to participants)
    ├── VaultCorp.csproj
    ├── Program.cs
    ├── appsettings.json
    ├── Dockerfile
    ├── docker-compose.yml
    ├── Data/
    │   ├── AppDbContext.cs
    │   └── Models/
    │       ├── Tenant.cs
    │       ├── User.cs
    │       └── Flag.cs
    ├── Services/
    │   ├── TenantService.cs
    │   └── UrlFetchService.cs
    ├── Jobs/
    │   ├── CommandExecutorJob.cs
    │   └── DatabaseCleanupJob.cs
    ├── Pages/
    │   ├── Shared/_Layout.cshtml
    │   ├── _ViewImports.cshtml
    │   ├── _ViewStart.cshtml
    │   ├── Index.cshtml
    │   ├── Register.cshtml
    │   ├── Login.cshtml
    │   ├── SupportLogin.cshtml
    │   ├── Dashboard.cshtml
    │   ├── Profile.cshtml
    │   ├── Logout.cshtml
    │   ├── Admin/
    │   │   ├── Index.cshtml
    │   │   └── Import.cshtml
    │   └── Assets/
    │       └── UserUtils.cshtml
    └── wwwroot/
        ├── css/site.css
        └── js/
            ├── site.js
            ├── user-utils.js      ← served dynamically (per-tenant)
            └── internal.js
```

---

## Where to Start

| If you want to… | Go to |
| --- | --- |
| Understand what the challenge tests | `documentation/Objective.md` |
| See the player-facing description (no spoilers) | `documentation/Public Briefing.md` |
| Reproduce the full solve step by step | `documentation/Walkthrough.md` |
| Understand the security root cause | `documentation/What Happened and Why (Root Cause).md` |
| See mitigation recommendations | `documentation/Mitigation.md` |
| Run the challenge | `bin/docker-compose.yml` |

---

## Running the Challenge

```bash
cd bin/
docker compose up
```

Then navigate to `http://localhost:5000`.

---

## Design Summary

- **Per-player isolation** — each `Register` creates a fully isolated tenant with unique admin credentials. The flag itself is baked into the deployed image at build time and is the same for every tenant in a given deployment (see `docs/challenge-design.mdx`, "Determinism and flag design") — isolation applies to account data, not to the flag value. Players cannot interfere with each other's tenant data.
- **Chain dependency** — no stage can be skipped. Stages 1→2→3→4→5→6 must be completed in order.
- **All real** — every vulnerability has a published CVE, OWASP entry, or documented bug bounty report. Nothing is contrived.
- **Single container** — no complex infrastructure. One `docker compose up` and the challenge is live.

---

## Vulnerability Chain

```
Stage 1  JS credential leak (XOR+Base64 obfuscation)
Stage 2  Hidden support login endpoint (directory brute-force)
Stage 3  SSRF with WAF bypass via IP obfuscation (127.1, decimal, octal, hex)
Stage 4  Internal Hangfire dashboard access via loopback trust (CVE-2021-41238)
Stage 5  Hangfire job injection → Remote Code Execution
Stage 6  Flag read via static file serving
```