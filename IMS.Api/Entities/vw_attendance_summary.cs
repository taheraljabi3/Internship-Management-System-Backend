using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class vw_attendance_summary
{
    public long? internship_id { get; set; }

    public long? student_user_id { get; set; }

    public string? student_name { get; set; }

    public string? provider_name { get; set; }

    public string? internship_title { get; set; }

    public decimal? total_hours { get; set; }

    public long? present_days { get; set; }

    public long? absent_days { get; set; }

    public DateOnly? last_attendance_date { get; set; }
}
