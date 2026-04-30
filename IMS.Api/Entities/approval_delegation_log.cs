using System;
using System.Collections.Generic;

namespace IMS.Api.Entities;

public partial class approval_delegation_log
{
    public long id { get; set; }

    public long request_id { get; set; }

    public long? student_user_id { get; set; }

    public long? from_owner_user_id { get; set; }

    public long to_owner_user_id { get; set; }

    public string reason { get; set; } = null!;

    public long changed_by_user_id { get; set; }

    public DateTime changed_at { get; set; }

    public virtual user changed_by_user { get; set; } = null!;

    public virtual user? from_owner_user { get; set; }

    public virtual user? student_user { get; set; }

    public virtual user to_owner_user { get; set; } = null!;
}
