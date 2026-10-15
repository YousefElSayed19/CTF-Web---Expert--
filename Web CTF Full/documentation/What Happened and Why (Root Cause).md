# What Happened and Why (Root Cause)

# What Happened and Why — Root Cause Analysis

---

## Overview

VaultCorp contains a chain of six distinct security vulnerabilities. None of them is catastrophic in isolation — but chained together, they lead to full Remote Code Execution on the server. This is the most common pattern in real-world breaches: not a single critical flaw, but a sequence of small mistakes that compound.

---

## Vulnerability 1 — Sensitive Data Exposure in Client-Side JavaScript

**What happened:**
The application generates per-tenant support credentials (admin LocalId + ApiToken) and embeds them in a JavaScript file served to authenticated users (`/js/user-utils.js`). While the data is obfuscated (XOR + Base64), the obfuscation algorithm and key are present in the same file.

**Root cause:**
A developer created a debugging fixture mechanism to test the support login flow during development. The fixtures were obfuscated “just enough” to avoid a plain `strings` scan, but the encoding is trivially reversible by anyone who reads the source. The code was never removed before deployment.

**Real-world analogy:**
Security credentials committed to a frontend bundle are a common finding in web application pentests. The encoding provides a false sense of security — “security through obscurity” is not a control.

**OWASP reference:** API3:2023 — Broken Object Property Level Authorization / A02:2021 — Cryptographic Failures

---

## Vulnerability 2 — Unauthenticated Support Endpoint

**What happened:**
A separate authentication endpoint (`/support_login`) accepts a LocalId + ApiToken pair and grants admin-level access. This endpoint is not linked from any UI element but is discoverable via directory brute-forcing.

**Root cause:**
The support team needed a way to access customer tenants without knowing their passwords. The endpoint was built for internal use but deployed to the public-facing server with no IP restriction, rate limiting, or additional authentication factor.

**Real-world analogy:**
“Hidden” admin panels are a frequent finding in web assessments. The assumption that “no one will find it if it’s not linked” is consistently proven wrong.

**OWASP reference:** API5:2023 — Broken Function Level Authorization

---

## Vulnerability 3 — Server-Side Request Forgery (SSRF) with Naive WAF

**What happened:**
The “Import Backup from URL” feature performs a server-side HTTP request to a URL provided by the user. A blacklist-based WAF blocks requests to `localhost`, `127.0.0.1`, `0.0.0.0`, and `[::1]` — but not to alternative representations of the same addresses (`127.1`, decimal, octal, hex).

**Root cause:**
The WAF performs string matching against a hardcoded blocklist rather than resolving and validating the destination IP address after DNS resolution. This is a well-documented SSRF filter bypass pattern covered in OWASP’s Web Security Testing Guide (WSTG-INPV-19).

**Real-world analogy:**
Naive SSRF filters are extremely common. Most developers add `localhost` to a blacklist and consider the problem solved, not realising that `127.1`, `0x7f000001`, and `2130706433` all resolve to the same loopback address.

**CVE reference:** Technique documented in OWASP WSTG-INPV-19 and multiple HackerOne reports.

---

## Vulnerability 4 — Internal Service Exposure via SSRF

**What happened:**
Using the SSRF bypass, the attacker can reach any port on the server’s loopback interface. The Hangfire background job dashboard is running on the same port as the main application but is protected only by a check that the request originates from a loopback address (`LocalRequestsOnlyAuthorizationFilter`). Since the SSRF causes the application to make the request to itself, the loopback check passes.

**Root cause:**
The `LocalRequestsOnlyAuthorizationFilter` is Hangfire’s default “protection” for development environments. It was left in place in production under the assumption that SSRF was not possible. Once SSRF is achieved, the filter provides no real protection.

**CVE reference:** CVE-2021-41238 — Hangfire.Core Missing Authorization (CVSS 8.6 HIGH)

---

## Vulnerability 5 — Unauthenticated Hangfire Job Injection (RCE)

**What happened:**
A developer-created internal API endpoint (`/internal/api/create`) allows creating arbitrary Hangfire jobs by specifying a .NET type name, method name, and arguments. The endpoint is protected only by the same loopback check. Through SSRF, the attacker can call this endpoint and inject a job that executes arbitrary shell commands.

**Root cause:**
Hangfire’s job execution model is inherently reflection-based — any registered .NET type can be invoked. The internal API was built for development tooling and was never removed. The `CommandExecutorJob` class was designed to run maintenance commands but was left accessible to the job injection API.

**Real-world analogy:**
Exposed Hangfire dashboards leading to RCE are documented in multiple published pentesting reports. When an attacker can create arbitrary jobs, they have effectively achieved code execution on the server process.

**Reference:** “Exploiting an Exposed Swagger File to Achieve RCE” — Mohamed Elhadad, 2025 (documents identical pattern: internal access → Hangfire credentials → RCE)

---

## Vulnerability 6 — Sensitive File Readable via Static Files

**What happened:**`CommandExecutorJob.Run` writes command output to the `wwwroot` directory, which is served as static files by ASP.NET Core. This allows the attacker to read the RCE output (including `/app/flag.txt`) via a simple HTTP request.

**Root cause:**
The job was designed to write output somewhere accessible for monitoring purposes. Writing to `wwwroot` was the simplest implementation — but it means any file written there is publicly readable.

---

## Chain Dependency Map

```
Vuln 1 (JS leak)
    → Vuln 2 (support login)
        → Vuln 3 (SSRF bypass)
            → Vuln 4 (Hangfire access)
                → Vuln 5 (job injection / RCE)
                    → Vuln 6 (flag readable via static files)
```

No vulnerability in the chain can be exploited without the previous one. This is what makes the challenge Hard rather than Medium.