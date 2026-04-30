namespace IMS.Api.DTOs
{
    public class ReviewDecisionRequest
    {
        public long actor_user_id { get; set; }
        public string? comment { get; set; }
    }

    public class DelegateApprovalRequest
    {
        public long changed_by_user_id { get; set; }
        public long to_owner_user_id { get; set; }
        public string to_owner_role { get; set; } = "Administrator";
        public string reason { get; set; } = string.Empty;
    }
}