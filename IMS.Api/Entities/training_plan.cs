using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class training_plan
{
    public long id { get; set; }

    public long internship_id { get; set; }

    public long student_user_id { get; set; }

    public DateOnly start_date { get; set; }

    public string plan_title { get; set; } = null!;

    public string plan_summary { get; set; } = null!;

    public long approval_owner_user_id { get; set; }

    public long assigned_advisor_user_id { get; set; }

    public string? approval_comment { get; set; }

    public DateTime submitted_at { get; set; }

    public DateTime? reviewed_at { get; set; }

    public long? reviewer_user_id { get; set; }

    public DateTime? approved_at { get; set; }

    public DateTime? rejected_at { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual user approval_owner_user { get; set; } = null!;

    public virtual user assigned_advisor_user { get; set; } = null!;

    public virtual internship internship { get; set; } = null!;

    public virtual user? reviewer_user { get; set; }

    public virtual user student_user { get; set; } = null!;

    public virtual ICollection<training_task> training_tasks { get; set; } = new List<training_task>();

    public virtual ICollection<weekly_report> weekly_reports { get; set; } = new List<weekly_report>();
}
