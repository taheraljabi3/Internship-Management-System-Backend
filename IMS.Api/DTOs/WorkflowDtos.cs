namespace IMS.Api.DTOs
{
    public class CreateTrainingCompanyRequestDto
    {
        public long student_user_id { get; set; }
        public string provider_name { get; set; } = string.Empty;
        public string provider_email { get; set; } = string.Empty;
        public string contact_name { get; set; } = string.Empty;
        public string contact_phone { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string sector { get; set; } = string.Empty;
        public string opportunity_title { get; set; } = string.Empty;
    }

    public class TrainingCompanyRequestListItemDto
    {
        public long id { get; set; }
        public long student_user_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string student_email { get; set; } = string.Empty;
        public string provider_name { get; set; } = string.Empty;
        public string provider_email { get; set; } = string.Empty;
        public string contact_name { get; set; } = string.Empty;
        public string contact_phone { get; set; } = string.Empty;
        public string city { get; set; } = string.Empty;
        public string sector { get; set; } = string.Empty;
        public string opportunity_title { get; set; } = string.Empty;
        public long approval_owner_user_id { get; set; }
        public string approval_owner_role { get; set; } = string.Empty;
        public string? approval_owner_name { get; set; }
        public long assigned_advisor_user_id { get; set; }
        public string? assigned_advisor_name { get; set; }
        public string status { get; set; } = string.Empty;
        public string? approval_comment { get; set; }
        public DateTime submitted_at { get; set; }
        public DateTime? reviewed_at { get; set; }
    }

    public class CreateTrainingPlanRequestDto
        {
        public long internship_id { get; set; }
        public long student_user_id { get; set; }

        public long? company_request_id { get; set; }
        public string? accepted_platform { get; set; }

        public DateOnly start_date { get; set; }
        public string plan_title { get; set; } = string.Empty;
        public string plan_summary { get; set; } = string.Empty;

        public string? attachment_file_name { get; set; }
        public string? attachment_file_url { get; set; }
        }

    public class TrainingPlanListItemDto
    {
        public long id { get; set; }
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string provider_name { get; set; } = string.Empty;
        public string internship_title { get; set; } = string.Empty;
        public DateOnly start_date { get; set; }
        public string plan_title { get; set; } = string.Empty;
        public string plan_summary { get; set; } = string.Empty;
        public long approval_owner_user_id { get; set; }
        public string approval_owner_role { get; set; } = string.Empty;
        public string? approval_owner_name { get; set; }
        public long assigned_advisor_user_id { get; set; }
        public string? assigned_advisor_name { get; set; }
        public string status { get; set; } = string.Empty;
        public string? approval_comment { get; set; }
        public DateTime submitted_at { get; set; }
        public DateTime? reviewed_at { get; set; }
    }

    public class NotificationListItemDto
    {
        public long id { get; set; }
        public long? user_id { get; set; }
        public string title { get; set; } = string.Empty;
        public string? message { get; set; }
        public string? recipient_email { get; set; }
        public string type { get; set; } = string.Empty;
        public string channel { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string? related_entity_type { get; set; }
        public long? related_entity_id { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? sent_at { get; set; }
        public DateTime? read_at { get; set; }
    }
    public class TrainingPlanInternshipLookupDto
{
    public long internship_id { get; set; }
    public long student_user_id { get; set; }
    public long? company_request_id { get; set; }
    public string provider_name { get; set; } = string.Empty;
}

public class TrainingPlanCompanyLookupDto
{
    public long id { get; set; }
    public long student_user_id { get; set; }
    public string provider_name { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public long assigned_advisor_user_id { get; set; }
    public long approval_owner_user_id { get; set; }
}


}