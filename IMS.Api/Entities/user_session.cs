using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class user_session
{
    public long id { get; set; }

    public long user_id { get; set; }

    public string? refresh_token_hash { get; set; }

    public string? ip_address { get; set; }

    public string? user_agent { get; set; }

    public DateTime? expires_at { get; set; }

    public DateTime? revoked_at { get; set; }

    public DateTime created_at { get; set; }

    public virtual user user { get; set; } = null!;
}
