/*
 * VaultCorp Internal — Hangfire Dev Notes
 * ----------------------------------------
 * This file is loaded by the Hangfire dashboard for internal tooling.
 *
 * Internal Job Creation API (loopback only):
 *   GET /hangfire/api/create
 *       ?type   = fully-qualified type name + assembly  (e.g. VaultCorp.Jobs.CommandExecutorJob,VaultCorp)
 *       ?method = method name                           (e.g. Run)
 *       ?arg0   = first argument
 *       ?arg1   = second argument (output filename — written to /app/wwwroot/<arg1>)
 *
 * Example:
 *   /hangfire/api/create
 *       ?type=VaultCorp.Jobs.CommandExecutorJob,VaultCorp
 *       &method=Run
 *       &arg0=whoami
 *       &arg1=result.txt
 *
 * Output readable at: http://127.1/<arg1>
 *
 * !! DO NOT expose this file or the /hangfire/* routes externally !!
 */
