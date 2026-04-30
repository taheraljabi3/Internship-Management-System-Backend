using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class email_outbox
{
    public long id { get; set; }

    public string to_email { get; set; } = null!;

    public string subject { get; set; } = null!;

    public string body { get; set; } = null!;

    public long? template_id { get; set; }

    public string? related_entity_type { get; set; }

    public long? related_entity_id { get; set; }

    public DateTime queued_at { get; set; }

    public DateTime? sent_at { get; set; }

    public string? error_message { get; set; }

    public virtual email_template? template { get; set; }
}
