using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using VaultCorp.Data;

namespace VaultCorp.Pages;

public class ProfileModel : PageModel
{
    private readonly AppDbContext _db;

    public string Username           { get; private set; } = string.Empty;
    public string Email              { get; private set; } = string.Empty;
    public string Role               { get; private set; } = string.Empty;
    public string DisplayName        { get; private set; } = string.Empty;
    public string CompanyName        { get; private set; } = string.Empty;
    public string Message            { get; private set; } = string.Empty;
    public string SupportFixturesJson { get; private set; } = "[]";

    public ProfileModel(AppDbContext db) => _db = db;

    public IActionResult OnGet()
    {
        var tenantId = HttpContext.Session.GetString("TenantId");
        if (tenantId is null) return RedirectToPage("/Login");

        LoadUser(tenantId);
        return Page();
    }

    public IActionResult OnPost(string displayName)
    {
        var tenantId = HttpContext.Session.GetString("TenantId");
        var userId   = HttpContext.Session.GetString("UserId");
        if (tenantId is null || userId is null) return RedirectToPage("/Login");

        var user = _db.Users.FirstOrDefault(u =>
            u.Id.ToString() == userId && u.TenantId == tenantId);

        if (user is not null)
        {
            user.DisplayName = displayName;
            _db.SaveChanges();
            Message = "Display name updated.";
        }

        LoadUser(tenantId);
        return Page();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void LoadUser(string tenantId)
    {
        var userId = HttpContext.Session.GetString("UserId");
        var user   = _db.Users.FirstOrDefault(u =>
            u.Id.ToString() == userId && u.TenantId == tenantId);

        if (user is null) return;

        Username    = user.Username;
        Email       = user.Email;
        Role        = user.Role;
        DisplayName = user.DisplayName;
        CompanyName = _db.Tenants.Find(tenantId)?.CompanyName ?? string.Empty;

        SupportFixturesJson = BuildFixtures(tenantId);
    }

    /// <summary>
    /// Builds the _supportFixtures array:
    ///   stg-01, stg-02 → random decoys (base64 of garbage bytes)
    ///   prod           → base64( "{adminLocalId}:tk_{adminToken}" ) — the real entry
    /// </summary>
    private string BuildFixtures(string tenantId)
    {
        var admin = _db.Users.FirstOrDefault(u =>
            u.TenantId == tenantId && u.Role == "Admin");

        string RandB64()
        {
            var buf = new byte[16];
            Random.Shared.NextBytes(buf);
            return Convert.ToBase64String(buf);
        }

        var prodPayload = admin is null
            ? RandB64()
            : Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{admin.LocalId}:tk_{admin.ApiToken}"));

        var fixtures = new[]
        {
            new { @ref = "stg-01", h = RandB64()   },
            new { @ref = "stg-02", h = RandB64()   },
            new { @ref = "prod",   h = prodPayload  }
        };

        // Shuffle so "prod" is not always last
        var list = fixtures.ToList();
        list.Sort((_, _) => Random.Shared.Next(-1, 2));

        return JsonSerializer.Serialize(list);
    }
}
