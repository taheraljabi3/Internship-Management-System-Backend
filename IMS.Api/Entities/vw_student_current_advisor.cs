using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class vw_student_current_advisor
{
    public long? student_user_id { get; set; }

    public long? advisor_user_id { get; set; }

    public string? student_name { get; set; }

    public string? student_email { get; set; }

    public string? advisor_name { get; set; }

    public string? advisor_email { get; set; }

    public DateTime? assignment_start_at { get; set; }

    public string? notes { get; set; }
}
