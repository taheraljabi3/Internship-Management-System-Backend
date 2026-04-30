using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class helper_opportunity_task
{
    public long id { get; set; }

    public long opportunity_id { get; set; }

    public string task_title { get; set; } = null!;

    public int sort_order { get; set; }

    public virtual helper_opportunity opportunity { get; set; } = null!;
}
