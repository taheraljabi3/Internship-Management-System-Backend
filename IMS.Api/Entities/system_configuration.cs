using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class system_configuration
{
    public long id { get; set; }

    public string key { get; set; } = null!;

    public string? value { get; set; }

    public string? category { get; set; }

    public string? description { get; set; }

    public bool is_editable { get; set; }

    public DateTime updated_at { get; set; }
}
