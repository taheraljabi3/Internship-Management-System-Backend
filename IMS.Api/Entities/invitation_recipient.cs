using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class invitation_recipient
{
    public long id { get; set; }

    public long batch_id { get; set; }

    public long advisor_user_id { get; set; }

    public string student_name { get; set; } = null!;

    public string? student_email { get; set; }

    public long? student_user_id { get; set; }

    public DateTime? sent_at { get; set; }

    public DateTime? accepted_at { get; set; }

    public DateTime created_at { get; set; }

    public virtual user advisor_user { get; set; } = null!;

    public virtual invitation_batch batch { get; set; } = null!;

    public virtual ICollection<student_eligibility_review> student_eligibility_reviews { get; set; } = new List<student_eligibility_review>();

    public virtual user? student_user { get; set; }
}
