using IMS.Api.Data;
using IMS.Api.DTOs;
using IMS.Api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InternshipContextController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InternshipContextController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyContext()
        {
            var studentUserId = User.GetUserId();
            var item = await GetStudentContextInternal(studentUserId);

            if (item == null)
                return NotFound(new { message = "No internship context found for this student." });

            return Ok(item);
        }

        [HttpGet("student/{studentUserId:long}")]
        [Authorize(Roles = "Administrator,AcademicAdvisor")]
        public async Task<IActionResult> GetStudentContext(long studentUserId)
        {
            var item = await GetStudentContextInternal(studentUserId);

            if (item == null)
                return NotFound(new { message = "No internship context found for this student." });

            return Ok(item);
        }

        private async Task<StudentInternshipContextDto?> GetStudentContextInternal(long studentUserId)
        {
            var sql = """
                SELECT
                    i.id AS internship_id,
                    i.student_user_id,
                    i.company_request_id,
                    i.provider_name,
                    i.provider_email,
                    i.internship_title,
                    i.status AS internship_status,
                    i.start_date,
                    i.end_date,

                    v.advisor_user_id,
                    v.advisor_name,
                    v.advisor_email,

                    lp.latest_training_plan_id,
                    lp.latest_training_plan_status,
                    lp.latest_training_plan_title,

                    lt.latest_task_id,
                    lt.latest_task_week_no,

                    lwr.latest_weekly_report_id,
                    lwr.latest_weekly_report_week_no,
                    lwr.latest_weekly_report_status,

                    lfer.latest_final_evaluation_request_id,

                    EXISTS (
                        SELECT 1
                        FROM company_evaluations ce
                        WHERE ce.internship_id = i.id
                    ) AS has_company_evaluation,

                    EXISTS (
                        SELECT 1
                        FROM academic_evaluations ae
                        WHERE ae.internship_id = i.id
                    ) AS has_academic_evaluation

                FROM internships i
                LEFT JOIN vw_student_current_advisor v
                    ON v.student_user_id = i.student_user_id

                LEFT JOIN LATERAL (
                    SELECT
                        p.id AS latest_training_plan_id,
                        p.status::text AS latest_training_plan_status,
                        p.plan_title AS latest_training_plan_title
                    FROM training_plans p
                    WHERE p.internship_id = i.id
                    ORDER BY p.submitted_at DESC, p.id DESC
                    LIMIT 1
                ) lp ON TRUE

                LEFT JOIN LATERAL (
                    SELECT
                        t.id AS latest_task_id,
                        t.week_no AS latest_task_week_no
                    FROM training_tasks t
                    WHERE t.internship_id = i.id
                    ORDER BY t.task_date DESC, t.id DESC
                    LIMIT 1
                ) lt ON TRUE

                LEFT JOIN LATERAL (
                    SELECT
                        w.id AS latest_weekly_report_id,
                        w.week_no AS latest_weekly_report_week_no,
                        w.status::text AS latest_weekly_report_status
                    FROM weekly_reports w
                    WHERE w.internship_id = i.id
                    ORDER BY w.generated_at DESC, w.id DESC
                    LIMIT 1
                ) lwr ON TRUE

                LEFT JOIN LATERAL (
                    SELECT
                        f.id AS latest_final_evaluation_request_id
                    FROM final_evaluation_requests f
                    WHERE f.internship_id = i.id
                    ORDER BY f.requested_at DESC, f.id DESC
                    LIMIT 1
                ) lfer ON TRUE

                WHERE i.student_user_id = @studentUserId
                ORDER BY
                    CASE
                        WHEN i.status IN ('Approved', 'InProgress') THEN 0
                        ELSE 1
                    END,
                    COALESCE(i.updated_at, i.created_at) DESC,
                    i.id DESC
                LIMIT 1
                """;

            var item = await _context.Database.SqlQueryRaw<StudentInternshipContextDto>(
                    sql,
                    new Npgsql.NpgsqlParameter("studentUserId", studentUserId))
                .FirstOrDefaultAsync();

            return item;
        }


    }
}