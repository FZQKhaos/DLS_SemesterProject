using System;
using System.Collections.Generic;

namespace Core.Domain.Entities;

public partial class User
{
    public string Id { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Hash { get; set; } = null!;

    public string Salt { get; set; } = null!;

    public string Role { get; set; } = null!;

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
}
