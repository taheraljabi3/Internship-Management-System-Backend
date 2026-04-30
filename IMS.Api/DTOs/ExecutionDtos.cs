namespace IMS.Api.DTOs
{
    public class TrainingTaskListItemDto
    {
        public long id { get; set; }
        public long training_plan_id { get; set; }
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public DateOnly task_date { get; set; }
        public int week_no { get; set; }
        public string task_title { get; set; } = string.Empty;
        public DateTime? created_at { get; set; }
        public DateTime? updated_at { get; set; }
        public int evidence_count { get; set; }
    }
    public class TrainingTaskEvidenceDto
    {
        public long id { get; set; }
        public long task_id { get; set; }
        public string file_name { get; set; } = string.Empty;
        public string? file_url { get; set; }
        public DateTime uploaded_at { get; set; }
    }

    public class CreateTrainingTaskRequest
    {
        public long internship_id { get; set; }
        public long training_plan_id { get; set; }
        public long student_user_id { get; set; }
        public DateOnly task_date { get; set; }
        public int week_no { get; set; }
        public string task_title { get; set; } = string.Empty;
    }

    public class AddTrainingTaskEvidenceRequest
    {
        public string file_name { get; set; } = string.Empty;
        public string? file_url { get; set; }
    }

    public class AttendanceEntryDto
    {
        public long id { get; set; }
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public DateOnly entry_date { get; set; }
        public TimeOnly? check_in_time { get; set; }
        public TimeOnly? check_out_time { get; set; }
        public decimal daily_hours { get; set; }
        public string status { get; set; } = string.Empty;
        public string? notes { get; set; }
        public DateTime created_at { get; set; }
    }

    public class AttendanceSummaryDto
    {
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string provider_name { get; set; } = string.Empty;
        public string internship_title { get; set; } = string.Empty;
        public decimal total_hours { get; set; }
        public long present_days { get; set; }
        public long absent_days { get; set; }
        public DateOnly? last_attendance_date { get; set; }
    }

    public class UpsertAttendanceEntryRequest
    {
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public DateOnly entry_date { get; set; }
        public TimeOnly? check_in_time { get; set; }
        public TimeOnly? check_out_time { get; set; }
        public string status { get; set; } = "Present";
        public string? notes { get; set; }
        public long? created_by_user_id { get; set; }
    }

    public class WeeklyReportListItemDto
    {
        public long id { get; set; }
        public long internship_id { get; set; }
        public long? training_plan_id { get; set; }
        public long student_user_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string provider_name { get; set; } = string.Empty;
        public string internship_title { get; set; } = string.Empty;
        public int week_no { get; set; }
        public DateOnly? week_start_date { get; set; }
        public DateOnly? week_end_date { get; set; }
        public string report_title { get; set; } = string.Empty;
        public string? report_summary { get; set; }
        public int total_tasks { get; set; }
        public int evidence_count { get; set; }
        public bool generated_from_tasks { get; set; }
        public long approval_owner_user_id { get; set; }
        public string approval_owner_role { get; set; } = string.Empty;
        public string? approval_owner_name { get; set; }
        public long assigned_advisor_user_id { get; set; }
        public string? assigned_advisor_name { get; set; }
        public string status { get; set; } = string.Empty;
        public string? approval_comment { get; set; }
        public DateTime generated_at { get; set; }
        public DateTime? reviewed_at { get; set; }
    }

    public class WeeklyReportItemDto
    {
        public long id { get; set; }
        public long weekly_report_id { get; set; }
        public long task_id { get; set; }
        public DateOnly task_date { get; set; }
        public int week_no { get; set; }
        public string task_title { get; set; } = string.Empty;
        public int evidence_count { get; set; }
    }

    public class GenerateWeeklyReportRequest
    {
        public long internship_id { get; set; }
        public int week_no { get; set; }
        public long requested_by_user_id { get; set; }
        public string? report_title { get; set; }
        public string? report_summary { get; set; }
    }

    public class FinalEvaluationRequestDto
    {
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public string provider_name { get; set; } = string.Empty;
        public string? provider_email { get; set; }
        public string sending_template_name { get; set; } = string.Empty;
        public string evaluation_template_name { get; set; } = string.Empty;
        public long? company_evaluation_template_id { get; set; }
        public long requested_by_user_id { get; set; }
    }
    public class RegisterCompanyEvaluationLinkRequest
    {
        public Guid token { get; set; }
        public string email { get; set; } = string.Empty;
        public string evaluator_name { get; set; } = string.Empty;
        public string evaluator_job_title { get; set; } = string.Empty;
    }

    public class CompanyEvaluationLinkLookupDto
    {
            public long id { get; set; }
    public long internship_id { get; set; }
    public long student_user_id { get; set; }
    public string student_name { get; set; } = string.Empty;
    public string provider_name { get; set; } = string.Empty;
    public string? provider_email { get; set; }
    public long? template_id { get; set; }
    public string status { get; set; } = string.Empty;
    public DateTime? token_expires_at { get; set; }

    public string? registered_email { get; set; }
    public DateTime? registered_at { get; set; }

    }

    public class FinalEvaluationRequestListItemDto
    {
        public long id { get; set; }
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        
        public string student_name { get; set; } = string.Empty;
        public string provider_name { get; set; } = string.Empty;
        public string? provider_email { get; set; }
        public string sending_template_name { get; set; } = string.Empty;
        public string evaluation_template_name { get; set; } = string.Empty;
        public long? company_evaluation_template_id { get; set; }
        public long requested_by_user_id { get; set; }
        public string requested_by_name { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public Guid? public_token { get; set; }
        public string? registration_link { get; set; }
        public DateTime requested_at { get; set; }
        public DateTime? completed_at { get; set; }
    }

    public class CompanyEvaluationTemplateDto
    {
        public long id { get; set; }
        public long? internship_id { get; set; }
        public long student_user_id { get; set; }
        public string provider_name { get; set; } = string.Empty;
        public string provider_email { get; set; } = string.Empty;
        public string title { get; set; } = string.Empty;
        public string version { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public string token { get; set; } = string.Empty;
        public long created_by_user_id { get; set; }
        public DateTime created_at { get; set; }
    }

    public class CompanyEvaluationTemplateCriterionDto
    {
        public long id { get; set; }
        public long template_id { get; set; }
        public string criterion_name { get; set; } = string.Empty;
        public decimal weight { get; set; }
        public int sort_order { get; set; }
    }

    public class CreateCompanyEvaluationTemplateRequest
    {
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public string provider_name { get; set; } = string.Empty;
        public string provider_email { get; set; } = string.Empty;
        public string title { get; set; } = "Company Evaluation Form";
        public string version { get; set; } = "v1.0";
        public long created_by_user_id { get; set; }
        public List<CreateCompanyEvaluationCriterionRequest> criteria { get; set; } = new();
    }

    public class CreateCompanyEvaluationCriterionRequest
    {
        public string criterion_name { get; set; } = string.Empty;
        public decimal weight { get; set; }
        public int sort_order { get; set; } = 1;
    }

    public class CompanyEvaluationListItemDto
    {
        public long id { get; set; }
        public long template_id { get; set; }
        public long? internship_id { get; set; }
        public long student_user_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string provider_name { get; set; } = string.Empty;
        public string? provider_email { get; set; }
        public string evaluator_name { get; set; } = string.Empty;
        public string evaluator_job_title { get; set; } = string.Empty;
        public string? notes { get; set; }
        public DateTime submitted_at { get; set; }
        public string status { get; set; } = string.Empty;
        public decimal total_percentage { get; set; }
    }

    public class CompanyEvaluationScoreInputDto
    {
        public long? criterion_id { get; set; }
        public string criterion_name { get; set; } = string.Empty;
        public decimal score { get; set; }
        public decimal out_of { get; set; }
    }

    public class SubmitCompanyEvaluationRequest
    {
        public long template_id { get; set; }
        public long? internship_id { get; set; }
        public long student_user_id { get; set; }
        public string provider_name { get; set; } = string.Empty;
        public string? provider_email { get; set; }
        public string evaluator_name { get; set; } = string.Empty;
        public string evaluator_job_title { get; set; } = string.Empty;
        public string? notes { get; set; }
        public Guid token { get; set; }
        public string email { get; set; } = string.Empty;
        public List<CompanyEvaluationScoreInputDto> scores { get; set; } = new();
    }

    public class AcademicEvaluationListItemDto
    {
        public long id { get; set; }
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public long? evaluator_user_id { get; set; }
        public string evaluator_name { get; set; } = string.Empty;
        public string? notes { get; set; }
        public DateTime submitted_at { get; set; }
        public string status { get; set; } = string.Empty;
        public decimal total_percentage { get; set; }
    }

    public class AcademicEvaluationScoreInputDto
    {
        public string criterion_name { get; set; } = string.Empty;
        public decimal score { get; set; }
        public decimal out_of { get; set; }
    }

    public class SubmitAcademicEvaluationRequest
    {
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public long? evaluator_user_id { get; set; }
        public string evaluator_name { get; set; } = string.Empty;
        public string? notes { get; set; }
        public List<AcademicEvaluationScoreInputDto> scores { get; set; } = new();
    }

    public class FinalEvaluationSummaryDto
    {
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string provider_name { get; set; } = string.Empty;
        public bool has_company_request { get; set; }
        public bool has_company_evaluation { get; set; }
        public bool has_academic_evaluation { get; set; }
        public decimal? company_total_percentage { get; set; }
        public decimal? academic_total_percentage { get; set; }
        public string? company_status { get; set; }
        public string? academic_status { get; set; }
    }
}