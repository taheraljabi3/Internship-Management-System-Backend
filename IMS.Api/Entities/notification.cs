using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class notification
{
    public long id { get; set; }

    public long? user_id { get; set; }

    public string title { get; set; } = null!;

    public string? message { get; set; }

    public string? recipient_email { get; set; }

    public string? related_entity_type { get; set; }

    public long? related_entity_id { get; set; }

    public DateTime created_at { get; set; }

    public DateTime? sent_at { get; set; }

    public DateTime? read_at { get; set; }

    public virtual user? user { get; set; }
}
