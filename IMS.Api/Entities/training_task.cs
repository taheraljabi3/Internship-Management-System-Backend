using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class training_task
{
    public long id { get; set; }

    public long internship_id { get; set; }

    public long training_plan_id { get; set; }

    public long student_user_id { get; set; }

    public DateOnly task_date { get; set; }

    public int week_no { get; set; }

    public string task_title { get; set; } = null!;

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual internship internship { get; set; } = null!;

    public virtual user student_user { get; set; } = null!;

    public virtual training_plan training_plan { get; set; } = null!;

    public virtual ICollection<training_task_evidence> training_task_evidences { get; set; } = new List<training_task_evidence>();

    public virtual ICollection<weekly_report_item> weekly_report_items { get; set; } = new List<weekly_report_item>();
}
