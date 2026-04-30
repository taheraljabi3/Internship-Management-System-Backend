using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class training_task_evidence
{
    public long id { get; set; }

    public long task_id { get; set; }

    public string file_name { get; set; } = null!;

    public string? file_url { get; set; }

    public DateTime uploaded_at { get; set; }

    public virtual training_task task { get; set; } = null!;
}
