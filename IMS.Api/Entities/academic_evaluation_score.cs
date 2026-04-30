using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class academic_evaluation_score
{
    public long id { get; set; }

    public long academic_evaluation_id { get; set; }

    public string criterion_name { get; set; } = null!;

    public decimal score { get; set; }

    public decimal out_of { get; set; }

    public virtual academic_evaluation academic_evaluation { get; set; } = null!;
}
