using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class user_role
{
    public long id { get; set; }

    public long user_id { get; set; }

    public long role_id { get; set; }

    public DateTime created_at { get; set; }

    public virtual role role { get; set; } = null!;

    public virtual user user { get; set; } = null!;
}
