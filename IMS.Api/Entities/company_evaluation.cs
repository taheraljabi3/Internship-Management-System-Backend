using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class company_evaluation
{
    public long id { get; set; }

    public long template_id { get; set; }

    public long? internship_id { get; set; }

    public long student_user_id { get; set; }

    public string provider_name { get; set; } = null!;

    public string? provider_email { get; set; }

    public string evaluator_name { get; set; } = null!;

    public string evaluator_job_title { get; set; } = null!;

    public string? notes { get; set; }

    public DateTime submitted_at { get; set; }

    public decimal total_percentage { get; set; }

    public virtual ICollection<company_evaluation_score> company_evaluation_scores { get; set; } = new List<company_evaluation_score>();

    public virtual internship? internship { get; set; }

    public virtual user student_user { get; set; } = null!;

    public virtual company_evaluation_template template { get; set; } = null!;
}
