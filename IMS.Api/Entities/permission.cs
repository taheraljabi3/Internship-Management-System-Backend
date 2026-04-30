using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class permission
{
    public long id { get; set; }

    public string code { get; set; } = null!;

    public string name { get; set; } = null!;

    public DateTime created_at { get; set; }

    public virtual ICollection<role_permission> role_permissions { get; set; } = new List<role_permission>();
}
