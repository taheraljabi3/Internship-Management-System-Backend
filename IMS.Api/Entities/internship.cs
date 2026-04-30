using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class internship
{
    public long id { get; set; }

    public long student_user_id { get; set; }

    public long company_request_id { get; set; }

    public string provider_name { get; set; } = null!;

    public string? provider_email { get; set; }

    public string internship_title { get; set; } = null!;

    public DateOnly? start_date { get; set; }

    public DateOnly? end_date { get; set; }

    public string status { get; set; } = null!;

    public bool approved_by_academic_advisor { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual academic_evaluation? academic_evaluation { get; set; }

    public virtual ICollection<attendance_entry> attendance_entries { get; set; } = new List<attendance_entry>();

    public virtual ICollection<company_evaluation_template> company_evaluation_templates { get; set; } = new List<company_evaluation_template>();

    public virtual ICollection<company_evaluation> company_evaluations { get; set; } = new List<company_evaluation>();

    public virtual training_company_request company_request { get; set; } = null!;

    public virtual ICollection<field_visit> field_visits { get; set; } = new List<field_visit>();

    public virtual ICollection<final_evaluation_request> final_evaluation_requests { get; set; } = new List<final_evaluation_request>();

    public virtual ICollection<online_follow_up> online_follow_ups { get; set; } = new List<online_follow_up>();

    public virtual user student_user { get; set; } = null!;

    public virtual ICollection<training_plan> training_plans { get; set; } = new List<training_plan>();

    public virtual ICollection<training_task> training_tasks { get; set; } = new List<training_task>();

    public virtual ICollection<weekly_report> weekly_reports { get; set; } = new List<weekly_report>();
}
