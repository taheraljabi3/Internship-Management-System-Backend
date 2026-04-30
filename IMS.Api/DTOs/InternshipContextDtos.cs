namespace IMS.Api.DTOs
{
    public class StudentInternshipContextDto
    {
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public long? company_request_id { get; set; }

        public string provider_name { get; set; } = string.Empty;
        public string? provider_email { get; set; }
        public string internship_title { get; set; } = string.Empty;
        public string internship_status { get; set; } = string.Empty;

        public DateOnly? start_date { get; set; }
        public DateOnly? end_date { get; set; }

        public long? advisor_user_id { get; set; }
        public string? advisor_name { get; set; }
        public string? advisor_email { get; set; }

        public long? latest_training_plan_id { get; set; }
        public string? latest_training_plan_status { get; set; }
        public string? latest_training_plan_title { get; set; }

        public long? latest_task_id { get; set; }
        public int? latest_task_week_no { get; set; }

        public long? latest_weekly_report_id { get; set; }
        public int? latest_weekly_report_week_no { get; set; }
        public string? latest_weekly_report_status { get; set; }

        public long? latest_final_evaluation_request_id { get; set; }

        public bool has_company_evaluation { get; set; }
        public bool has_academic_evaluation { get; set; }
    }
}