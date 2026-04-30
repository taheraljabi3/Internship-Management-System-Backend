namespace IMS.Api.DTOs
{
    public class HelperProviderDto
    {
        public long id { get; set; }
        public string name { get; set; } = string.Empty;
        public string? sector { get; set; }
        public string? email { get; set; }
        public string? phone { get; set; }
        public string? city { get; set; }
        public string? website_url { get; set; }
        public string? description { get; set; }
    }

    public class HelperOpportunityListItemDto
    {
        public long id { get; set; }
        public long? provider_id { get; set; }
        public string title { get; set; } = string.Empty;
        public string? provider_name { get; set; }
        public string? location { get; set; }
        public string? work_mode { get; set; }
        public string? source { get; set; }
        public string? description { get; set; }
        public DateTime? deadline_at { get; set; }
        public string status { get; set; } = string.Empty;
    }

    public class HelperOpportunityDetailsDto : HelperOpportunityListItemDto
    {
        public List<string> requirements { get; set; } = new();
        public List<string> tasks { get; set; } = new();
    }
}