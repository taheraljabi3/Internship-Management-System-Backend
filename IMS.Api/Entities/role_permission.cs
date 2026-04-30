using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class role_permission
{
    public long id { get; set; }

    public long role_id { get; set; }

    public long permission_id { get; set; }

    public DateTime created_at { get; set; }

    public virtual permission permission { get; set; } = null!;

    public virtual role role { get; set; } = null!;
}
