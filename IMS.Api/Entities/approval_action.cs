using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class approval_action
{
    public long id { get; set; }

    public long request_id { get; set; }

    public long? student_user_id { get; set; }

    public long actor_user_id { get; set; }

    public string? comment { get; set; }

    public DateTime acted_at { get; set; }

    public virtual user actor_user { get; set; } = null!;

    public virtual user? student_user { get; set; }
}
