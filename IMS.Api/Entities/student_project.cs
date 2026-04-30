using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class student_project
{
    public long id { get; set; }

    public long student_user_id { get; set; }

    public string title { get; set; } = null!;

    public int? project_year { get; set; }

    public string? role_name { get; set; }

    public string? project_link { get; set; }

    public string? description { get; set; }

    public DateTime created_at { get; set; }

    public virtual user student_user { get; set; } = null!;
}
