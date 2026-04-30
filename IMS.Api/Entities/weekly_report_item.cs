using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class weekly_report_item
{
    public long id { get; set; }

    public long weekly_report_id { get; set; }

    public long task_id { get; set; }

    public virtual training_task task { get; set; } = null!;

    public virtual weekly_report weekly_report { get; set; } = null!;
}
