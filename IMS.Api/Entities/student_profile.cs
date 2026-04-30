using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class student_profile
{
    public long user_id { get; set; }

    public string? student_code { get; set; }

    public string? headline { get; set; }

    public string? university { get; set; }

    public string? major { get; set; }

    public decimal? gpa { get; set; }

    public string? city { get; set; }

    public string? country { get; set; }

    public int? graduation_year { get; set; }

    public string? linked_in_url { get; set; }

    public string? photo_url { get; set; }

    public string? bio { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual user user { get; set; } = null!;
}
