namespace IMS.Api.DTOs
{
    public class AdvisorListItemDto
    {
        public long user_id { get; set; }
        public string full_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string? employee_no { get; set; }
        public string? department { get; set; }
        public bool is_system_responsible { get; set; }
        public int students_count { get; set; }
    }

    public class AdvisorStudentItemDto
    {
        public long student_user_id { get; set; }
        public string full_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string? student_code { get; set; }
        public string? university { get; set; }
        public string? major { get; set; }
        public decimal? gpa { get; set; }
        public DateTime? assignment_start_at { get; set; }        public string? notes { get; set; }
    }

    public class AssignStudentAdvisorRequest
    {
        public long student_user_id { get; set; }
        public long advisor_user_id { get; set; }
        public long assigned_by_user_id { get; set; }
        public string? notes { get; set; }
    }
}