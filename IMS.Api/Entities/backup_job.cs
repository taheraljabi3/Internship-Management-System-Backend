using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class backup_job
{
    public long id { get; set; }

    public string job_name { get; set; } = null!;

    public string? schedule { get; set; }

    public DateTime? last_run_at { get; set; }

    public DateTime created_at { get; set; }
}
