using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class student_eligibility_review
{
    public long id { get; set; }

    public long student_user_id { get; set; }

    public long? invitation_recipient_id { get; set; }

    public long? advisor_assignment_id { get; set; }

    public long approval_owner_user_id { get; set; }

    public long? reviewer_user_id { get; set; }

    public string? comment { get; set; }

    public DateTime? reviewed_at { get; set; }

    public DateTime? approved_at { get; set; }

    public DateTime? rejected_at { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual advisor_student_assignment? advisor_assignment { get; set; }

    public virtual user approval_owner_user { get; set; } = null!;

    public virtual invitation_recipient? invitation_recipient { get; set; }

    public virtual user? reviewer_user { get; set; }

    public virtual user student_user { get; set; } = null!;
}
