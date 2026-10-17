# Maintainer docs (staged for openlabs)

These 4 files satisfy sub-issue #160's "maintainer docs" checklist item.
They follow the real convention found in `duckurity/openlabs` at
`labs/web/firmdrama/docs/` (the only existing lab currently using this
exact 4-file set), scaled down to VaultCorp's actual scope — a single
ASP.NET Core container with 6 chained vulnerabilities, versus firmdrama's
larger multi-service GraphQL lab.

Reorganized from the original narrative write-ups under `../documentation/`
(`Objective.md`, `Walkthrough.md`, `What Happened and Why (Root
Cause).md`, `Mitigation.md`) into the catalog's maintainer-doc structure.
`../documentation/Public Briefing.md` and `Walkthrough.md` are kept as-is
— they serve a different purpose (player-facing brief / screenshotted
solve) and aren't duplicated here.

Staged here only, per the standing rule: nothing gets pushed to
`duckurity/openlabs` until all 3 sub-issues are agreed with the maintainer.
Final home once agreed:

```
labs/web/vaultcorp-ssrf-rce/docs/challenge-design.mdx
labs/web/vaultcorp-ssrf-rce/docs/security-report.mdx
labs/web/vaultcorp-ssrf-rce/docs/operations.mdx
labs/web/vaultcorp-ssrf-rce/docs/validation-report.mdx
```
