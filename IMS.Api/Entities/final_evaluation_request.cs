using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class final_evaluation_request
{
    public long id { get; set; }

    public long internship_id { get; set; }

    public long student_user_id { get; set; }

    public long? company_evaluation_template_id { get; set; }
    public string provider_name { get; set; } = null!;

    public string? provider_email { get; set; }

    public string sending_template_name { get; set; } = null!;

    public string evaluation_template_name { get; set; } = null!;

    public long requested_by_user_id { get; set; }

    public DateTime requested_at { get; set; }

    public DateTime? completed_at { get; set; }

    public DateTime created_at { get; set; }

    public virtual company_evaluation_template? company_evaluation_template { get; set; }

    public virtual internship internship { get; set; } = null!;

    public virtual user requested_by_user { get; set; } = null!;

    public virtual user student_user { get; set; } = null!;
}
