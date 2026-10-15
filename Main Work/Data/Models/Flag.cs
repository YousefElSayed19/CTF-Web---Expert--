namespace VaultCorp.Data.Models;

public class Flag
{
    public int    Id       { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string Value    { get; set; } = string.Empty;
}
