namespace IMS.Api.DTOs
{
    public class StudentListItemDto
    {
        public long user_id { get; set; }
        public string? full_name { get; set; }
        public string? email { get; set; }

        public string? student_code { get; set; }
        public string? university { get; set; }
        public string? major { get; set; }
        public decimal? gpa { get; set; }

        public long? advisor_user_id { get; set; }
        public string? advisor_name { get; set; }
        public string? advisor_email { get; set; }
    }
}