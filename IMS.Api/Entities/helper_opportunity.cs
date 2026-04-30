using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class helper_opportunity
{
    public long id { get; set; }

    public long? provider_id { get; set; }

    public string title { get; set; } = null!;

    public string? location { get; set; }

    public string? work_mode { get; set; }

    public string? source { get; set; }

    public string? description { get; set; }

    public DateTime? deadline_at { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual ICollection<helper_opportunity_requirement> helper_opportunity_requirements { get; set; } = new List<helper_opportunity_requirement>();

    public virtual ICollection<helper_opportunity_task> helper_opportunity_tasks { get; set; } = new List<helper_opportunity_task>();

    public virtual helper_provider? provider { get; set; }
}
