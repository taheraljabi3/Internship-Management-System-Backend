namespace IMS.Api.DTOs
{
    public class CreateEligibilityReviewRequest
    {
        public long? student_user_id { get; set; }
        public long? invitation_recipient_id { get; set; }
        public long? advisor_assignment_id { get; set; }
        public long? approval_owner_user_id { get; set; }
        public string approval_owner_role { get; set; } = "AcademicAdvisor";
    }

    public class EligibilityListItemDto
    {
        public long id { get; set; }
        public long student_user_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string student_email { get; set; } = string.Empty;
        public long approval_owner_user_id { get; set; }
        public string approval_owner_role { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string? comment { get; set; }
        public DateTime created_at { get; set; }
        public string? advisor_name { get; set; }
    }
        public class InvitationEligibilityLookupDto
    {
        public long invitation_recipient_id { get; set; }
        public string? student_email { get; set; }
        public long advisor_user_id { get; set; }
    }

}