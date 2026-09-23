using System.ComponentModel.DataAnnotations;

namespace DraftService.Domain;

public class Draft
{
    [Required]
    public string Id { get; set; }
    
    [Required]
    public string Title { get; set; }
    
    [Required]
    public string Body { get; set; }
    
    [Required]
    public string Continent { get; set; }
}