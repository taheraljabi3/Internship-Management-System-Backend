using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class invitation_batch
{
    public long id { get; set; }

    public long advisor_user_id { get; set; }

    public long created_by_user_id { get; set; }

    public string? excel_file_name { get; set; }

    public string? shared_link_token { get; set; }

    public string? shared_link_url { get; set; }

    public string invitation_message { get; set; } = null!;

    public int total_recipients { get; set; }

    public DateTime? sent_at { get; set; }

    public DateTime created_at { get; set; }

    public virtual user advisor_user { get; set; } = null!;

    public virtual user created_by_user { get; set; } = null!;

    public virtual ICollection<invitation_recipient> invitation_recipients { get; set; } = new List<invitation_recipient>();
}
