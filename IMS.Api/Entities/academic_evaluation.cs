using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class academic_evaluation
{
    public long id { get; set; }

    public long internship_id { get; set; }

    public long student_user_id { get; set; }

    public long? evaluator_user_id { get; set; }

    public string evaluator_name { get; set; } = null!;

    public string? notes { get; set; }

    public DateTime submitted_at { get; set; }

    public decimal total_percentage { get; set; }

    public virtual ICollection<academic_evaluation_score> academic_evaluation_scores { get; set; } = new List<academic_evaluation_score>();

    public virtual user? evaluator_user { get; set; }

    public virtual internship internship { get; set; } = null!;

    public virtual user student_user { get; set; } = null!;
}
