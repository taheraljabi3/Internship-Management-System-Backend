using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class company_evaluation_template_criterion
{
    public long id { get; set; }

    public long template_id { get; set; }

    public string criterion_name { get; set; } = null!;

    public decimal weight { get; set; }

    public int sort_order { get; set; }

    public virtual ICollection<company_evaluation_score> company_evaluation_scores { get; set; } = new List<company_evaluation_score>();

    public virtual company_evaluation_template template { get; set; } = null!;
}
