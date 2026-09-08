using System.ComponentModel.DataAnnotations;

// ReSharper disable InconsistentNaming

namespace Application.Models;

public sealed class AppOptions
{
    [Required] public string DbConnectionString { get; set; } = string.Empty!;
    public bool Seed { get; set; } = true;
    public int PORT { get; set; } = 8080;
    public int REST_PORT { get; set; } = 5000;
}
