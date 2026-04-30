using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class student_skill
{
    public long id { get; set; }

    public long student_user_id { get; set; }

    public string name { get; set; } = null!;

    public string? category { get; set; }

    public DateTime created_at { get; set; }

    public virtual user student_user { get; set; } = null!;
}
