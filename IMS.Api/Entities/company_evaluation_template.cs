using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class company_evaluation_template
{
    public long id { get; set; }

    public long? internship_id { get; set; }

    public long student_user_id { get; set; }

    public string provider_name { get; set; } = null!;

    public string provider_email { get; set; } = null!;

    public string title { get; set; } = null!;

    public string version { get; set; } = null!;

    public string token { get; set; } = null!;

    public long created_by_user_id { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual ICollection<company_evaluation_template_criterion> company_evaluation_template_criteria { get; set; } = new List<company_evaluation_template_criterion>();

    public virtual ICollection<company_evaluation> company_evaluations { get; set; } = new List<company_evaluation>();

    public virtual user created_by_user { get; set; } = null!;

    public virtual ICollection<final_evaluation_request> final_evaluation_requests { get; set; } = new List<final_evaluation_request>();

    public virtual internship? internship { get; set; }

    public virtual user student_user { get; set; } = null!;
}
