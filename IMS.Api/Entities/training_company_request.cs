using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class training_company_request
{
    public long id { get; set; }

    public long student_user_id { get; set; }

    public long? advisor_assignment_id { get; set; }

    public string provider_name { get; set; } = null!;

    public string provider_email { get; set; } = null!;

    public string contact_name { get; set; } = null!;

    public string contact_phone { get; set; } = null!;

    public string city { get; set; } = null!;

    public string sector { get; set; } = null!;

    public string opportunity_title { get; set; } = null!;

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

    public virtual advisor_student_assignment? advisor_assignment { get; set; }

    public virtual user approval_owner_user { get; set; } = null!;

    public virtual user assigned_advisor_user { get; set; } = null!;

    public virtual internship? internship { get; set; }

    public virtual user? reviewer_user { get; set; }

    public virtual user student_user { get; set; } = null!;
}
