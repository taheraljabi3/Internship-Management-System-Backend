using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class helper_opportunity_requirement
{
    public long id { get; set; }

    public long opportunity_id { get; set; }

    public string requirement_text { get; set; } = null!;

    public int sort_order { get; set; }

    public virtual helper_opportunity opportunity { get; set; } = null!;
}
