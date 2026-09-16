using System;
using System.Collections.Generic;

namespace Core.Domain.Entities;

public partial class Group
{
    public string Id { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
