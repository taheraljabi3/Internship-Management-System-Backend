using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class company_evaluation_score
{
    public long id { get; set; }

    public long company_evaluation_id { get; set; }

    public long? criterion_id { get; set; }

    public string criterion_name { get; set; } = null!;

    public decimal score { get; set; }

    public decimal out_of { get; set; }

    public virtual company_evaluation company_evaluation { get; set; } = null!;

    public virtual company_evaluation_template_criterion? criterion { get; set; }
}
