using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class advisor_student_assignment
{
    public long id { get; set; }

    public long student_user_id { get; set; }

    public long advisor_user_id { get; set; }

    public long? assigned_by_user_id { get; set; }

    public DateTime assignment_start_at { get; set; }

    public DateTime? assignment_end_at { get; set; }

    public string? notes { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual user advisor_user { get; set; } = null!;

    public virtual user? assigned_by_user { get; set; }

    public virtual ICollection<student_eligibility_review> student_eligibility_reviews { get; set; } = new List<student_eligibility_review>();

    public virtual user student_user { get; set; } = null!;

    public virtual ICollection<training_company_request> training_company_requests { get; set; } = new List<training_company_request>();
}
