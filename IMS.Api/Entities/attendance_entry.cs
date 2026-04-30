using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class attendance_entry
{
    public long id { get; set; }

    public long internship_id { get; set; }

    public long student_user_id { get; set; }

    public DateOnly entry_date { get; set; }

    public TimeOnly? check_in_time { get; set; }

    public TimeOnly? check_out_time { get; set; }

    public decimal daily_hours { get; set; }

    public string? notes { get; set; }

    public long? created_by_user_id { get; set; }

    public DateTime created_at { get; set; }

    public DateTime updated_at { get; set; }

    public virtual user? created_by_user { get; set; }

    public virtual internship internship { get; set; } = null!;

    public virtual user student_user { get; set; } = null!;
}
