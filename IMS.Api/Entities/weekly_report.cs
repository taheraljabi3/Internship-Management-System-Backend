using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class weekly_report
{
    public long id { get; set; }

    public long internship_id { get; set; }

    public long? training_plan_id { get; set; }

    public long student_user_id { get; set; }

    public int week_no { get; set; }

    public DateOnly? week_start_date { get; set; }

    public DateOnly? week_end_date { get; set; }

    public string report_title { get; set; } = null!;

    public string? report_summary { get; set; }

    public int total_tasks { get; set; }

    public int evidence_count { get; set; }

    public bool generated_from_tasks { get; set; }

    public DateTime generated_at { get; set; }

    public long approval_owner_user_id { get; set; }

    public long assigned_advisor_user_id { get; set; }

    public string? approval_comment { get; set; }

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

    public virtual training_plan? training_plan { get; set; }

    public virtual ICollection<weekly_report_item> weekly_report_items { get; set; } = new List<weekly_report_item>();
}
