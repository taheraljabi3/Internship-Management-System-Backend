using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class administrator_profile
{
    public long user_id { get; set; }

    public string? employee_no { get; set; }

    public string? department { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual user user { get; set; } = null!;
}
