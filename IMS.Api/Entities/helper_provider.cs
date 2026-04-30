using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class helper_provider
{
    public long id { get; set; }

    public string name { get; set; } = null!;

    public string? sector { get; set; }

    public string? email { get; set; }

    public string? phone { get; set; }

    public string? city { get; set; }

    public string? website_url { get; set; }

    public string? description { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual ICollection<helper_opportunity> helper_opportunities { get; set; } = new List<helper_opportunity>();
}
