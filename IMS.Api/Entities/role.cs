using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class role
{
    public long id { get; set; }

    public string code { get; set; } = null!;

    public string name { get; set; } = null!;

    public DateTime created_at { get; set; }

    public virtual ICollection<role_permission> role_permissions { get; set; } = new List<role_permission>();

    public virtual ICollection<user_role> user_roles { get; set; } = new List<user_role>();
}
