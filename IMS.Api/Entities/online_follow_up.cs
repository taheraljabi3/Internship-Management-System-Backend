using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class online_follow_up
{
    public long id { get; set; }

    public long internship_id { get; set; }

    public long advisor_user_id { get; set; }

    public DateOnly schedule_date { get; set; }

    public TimeOnly? schedule_time { get; set; }

    public string? meeting_link { get; set; }

    public string? notes { get; set; }

    public string? status { get; set; }

    public DateTime created_at { get; set; }

    public virtual user advisor_user { get; set; } = null!;

    public virtual internship internship { get; set; } = null!;
}
