namespace IMS.Api.DTOs
{
    public class InvitationRecipientInputDto
    {
        public string student_name { get; set; } = string.Empty;
        public string? student_email { get; set; }
    }

    public class CreateInvitationBatchRequest
    {
        public string invitation_mode { get; set; } = "Excel"; // Excel / Link
        public long advisor_user_id { get; set; }
        public long created_by_user_id { get; set; }
        public string? excel_file_name { get; set; }
        public string? shared_link_token { get; set; }
        public string? shared_link_url { get; set; }
        public string invitation_message { get; set; } = "You have been invited to access the internship training platform. Please log in and complete your profile.";
        public List<InvitationRecipientInputDto> recipients { get; set; } = new();
    }

    public class InvitationBatchListItemDto
    {
        public long id { get; set; }
        public string invitation_mode { get; set; } = string.Empty;
        public long advisor_user_id { get; set; }
        public string advisor_name { get; set; } = string.Empty;
        public string? excel_file_name { get; set; }
        public string? shared_link_url { get; set; }
        public int total_recipients { get; set; }
        public DateTime? sent_at { get; set; }
        public DateTime created_at { get; set; }
    }

    public class InvitationRecipientListItemDto
    {
        public long id { get; set; }
        public long batch_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string? student_email { get; set; }
        public long? student_user_id { get; set; }
        public string invitation_status { get; set; } = string.Empty;
        public DateTime? sent_at { get; set; }
        public DateTime? accepted_at { get; set; }
    }
}