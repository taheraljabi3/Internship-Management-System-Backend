using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class audit_log
{
    public long id { get; set; }

    public long? actor_user_id { get; set; }

    public string action { get; set; } = null!;

    public string entity_name { get; set; } = null!;

    public string? entity_id { get; set; }

    public string? metadata { get; set; }

    public DateTime created_at { get; set; }

    public virtual user? actor_user { get; set; }
}
