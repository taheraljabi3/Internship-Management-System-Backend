using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class email_template
{
    public long id { get; set; }

    public string name { get; set; } = null!;

    public string? category { get; set; }

    public string subject_template { get; set; } = null!;

    public string body_template { get; set; } = null!;

    public bool is_active { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual ICollection<email_outbox> email_outboxes { get; set; } = new List<email_outbox>();
}
