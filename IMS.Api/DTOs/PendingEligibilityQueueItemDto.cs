namespace IMS.Api.DTOs
{
    public class PendingEligibilityQueueItemDto
    {
        public long invitation_recipient_id { get; set; }
        public long invitation_batch_id { get; set; }

        public long? eligibility_review_id { get; set; }

        public long? student_user_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string? student_email { get; set; }

        public long advisor_user_id { get; set; }
        public string advisor_name { get; set; } = string.Empty;
        public string advisor_email { get; set; } = string.Empty;

        public string invitation_status { get; set; } = string.Empty;
        public string eligibility_status { get; set; } = "Pending";
        public string? user_status { get; set; }

        public bool account_created { get; set; }
        public DateTime queue_created_at { get; set; }
    }
}