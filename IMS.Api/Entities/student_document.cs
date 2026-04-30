using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class student_document
{
    public long id { get; set; }

    public long student_user_id { get; set; }

    public string title { get; set; } = null!;

    public string file_name { get; set; } = null!;

    public string? file_url { get; set; }

    public DateTime uploaded_at { get; set; }

    public string? description { get; set; }

    public long? generated_by_user_id { get; set; }

    public virtual user? generated_by_user { get; set; }

    public virtual user student_user { get; set; } = null!;
}
