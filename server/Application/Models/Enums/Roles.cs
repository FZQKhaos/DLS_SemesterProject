using System.ComponentModel.DataAnnotations;

namespace Application.Models.Enums;

public static class Constants
{
    [Required] public static string UserRole = "user";

    [Required] public static string AdminRole = "admin";
}