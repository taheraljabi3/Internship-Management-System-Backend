using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class archived_record
{
    public long id { get; set; }

    public string entity_name { get; set; } = null!;

    public string record_reference { get; set; } = null!;

    public string? reason { get; set; }

    public string? payload { get; set; }

    public DateTime archived_at { get; set; }

    public long? archived_by_user_id { get; set; }

    public virtual user? archived_by_user { get; set; }
}
