using IMS.Api.Data;
using IMS.Api.DTOs;
using IMS.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeeklyReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WeeklyReportsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("internship/{internshipId:long}")]
        public async Task<IActionResult> GetByInternship(long internshipId)
        {
            var sql = """
                SELECT
                    w.id,
                    w.internship_id,
                    w.training_plan_id,
                    w.student_user_id,
                    su.full_name AS student_name,
                    i.provider_name,
                    i.internship_title,
                    w.week_no,
                    w.week_start_date,
                    w.week_end_date,
                    w.report_title,
                    w.report_summary,
                    w.total_tasks,
                    w.evidence_count,
                    w.generated_from_tasks,
                    w.approval_owner_user_id,
                    w.approval_owner_role::text AS approval_owner_role,
                    ou.full_name AS approval_owner_name,
                    w.assigned_advisor_user_id,
                    au.full_name AS assigned_advisor_name,
                    w.status::text AS status,
                    w.approval_comment,
                    w.generated_at,
                    w.reviewed_at
                FROM weekly_reports w
                JOIN internships i ON i.id = w.internship_id
                JOIN users su ON su.id = w.student_user_id
                JOIN users ou ON ou.id = w.approval_owner_user_id
                JOIN users au ON au.id = w.assigned_advisor_user_id
                WHERE w.internship_id = @internshipId
                ORDER BY w.week_no DESC;
                """;

            var items = await _context.Database.SqlQueryRaw<WeeklyReportListItemDto>(
                sql,
                new Npgsql.NpgsqlParameter("internshipId", internshipId))
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("owner/{ownerUserId:long}")]
        public async Task<IActionResult> GetByOwner(long ownerUserId)
        {
            var sql = """
                SELECT
                    w.id,
                    w.internship_id,
                    w.training_plan_id,
                    w.student_user_id,
                    su.full_name AS student_name,
                    i.provider_name,
                    i.internship_title,
                    w.week_no,
                    w.week_start_date,
                    w.week_end_date,
                    w.report_title,
                    w.report_summary,
                    w.total_tasks,
                    w.evidence_count,
                    w.generated_from_tasks,
                    w.approval_owner_user_id,
                    w.approval_owner_role::text AS approval_owner_role,
                    ou.full_name AS approval_owner_name,
                    w.assigned_advisor_user_id,
                    au.full_name AS assigned_advisor_name,
                    w.status::text AS status,
                    w.approval_comment,
                    w.generated_at,
                    w.reviewed_at
                FROM weekly_reports w
                JOIN internships i ON i.id = w.internship_id
                JOIN users su ON su.id = w.student_user_id
                JOIN users ou ON ou.id = w.approval_owner_user_id
                JOIN users au ON au.id = w.assigned_advisor_user_id
                WHERE w.approval_owner_user_id = @ownerUserId
                ORDER BY w.generated_at DESC;
                """;

            var items = await _context.Database.SqlQueryRaw<WeeklyReportListItemDto>(
                sql,
                new Npgsql.NpgsqlParameter("ownerUserId", ownerUserId))
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{reportId:long}/items")]
        public async Task<IActionResult> GetItems(long reportId)
        {
            var sql = """
                SELECT
                    wri.id,
                    wri.weekly_report_id,
                    wri.task_id,
                    t.task_date,
                    t.week_no,
                    t.task_title,
                    COUNT(e.id)::int AS evidence_count
                FROM weekly_report_items wri
                JOIN training_tasks t ON t.id = wri.task_id
                LEFT JOIN training_task_evidences e ON e.task_id = t.id
                WHERE wri.weekly_report_id = @reportId
                GROUP BY wri.id, t.id
                ORDER BY t.task_date, t.id;
                """;

            var items = await _context.Database.SqlQueryRaw<WeeklyReportItemDto>(
                sql,
                new Npgsql.NpgsqlParameter("reportId", reportId))
                .ToListAsync();

            return Ok(items);
        }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate(GenerateWeeklyReportRequest request)
    {
        if (request.internship_id <= 0)
            return BadRequest(new { message = "internship_id is required." });

        if (request.week_no <= 0)
            return BadRequest(new { message = "week_no must be greater than 0." });

        if (request.requested_by_user_id <= 0)
            return BadRequest(new { message = "requested_by_user_id is required." });

        var internship = await _context.Database
            .SqlQueryRaw<WeeklyReportInternshipLookupDto>(
                """
                SELECT
                    i.id AS internship_id,
                    i.student_user_id,
                    COALESCE(tp.id, 0) AS training_plan_id,
                    tp.approval_owner_user_id,
                    tp.assigned_advisor_user_id
                FROM internships i
                LEFT JOIN training_plans tp
                    ON tp.internship_id = i.id
                AND tp.status = 'Approved'::approval_status_enum
                WHERE i.id = @internshipId
                LIMIT 1
                """,
                new Npgsql.NpgsqlParameter("internshipId", request.internship_id))
            .FirstOrDefaultAsync();

        if (internship == null)
            return BadRequest(new { message = "Internship was not found." });

        if (internship.training_plan_id <= 0)
            return BadRequest(new { message = "Approved training plan is required before generating a weekly report." });

        if (internship.approval_owner_user_id <= 0 || internship.assigned_advisor_user_id <= 0)
            return BadRequest(new { message = "Approval owner / assigned advisor could not be resolved." });

        var existingReportId = _context.Database.SqlQuery<long>($"""
            SELECT id AS "Value"
            FROM weekly_reports
            WHERE internship_id = {request.internship_id}
            AND week_no = {request.week_no}
            LIMIT 1
            """).AsEnumerable().FirstOrDefault();

        if (existingReportId > 0)
            return BadRequest(new { message = "Weekly report for this week already exists." });

        var taskRows = await _context.Database
            .SqlQueryRaw<WeeklyReportTaskRowDto>(
                """
                SELECT
                    t.id AS task_id,
                    t.task_date,
                    COALESCE(ev.evidence_count, 0) AS evidence_count
                FROM training_tasks t
                LEFT JOIN
                (
                    SELECT
                        task_id,
                        COUNT(*)::int AS evidence_count
                    FROM training_task_evidences
                    GROUP BY task_id
                ) ev
                    ON ev.task_id = t.id
                WHERE t.internship_id = @internshipId
                AND t.week_no = @weekNo
                ORDER BY t.task_date, t.id
                """,
                new Npgsql.NpgsqlParameter("internshipId", request.internship_id),
                new Npgsql.NpgsqlParameter("weekNo", request.week_no))
            .ToListAsync();

        var totalTasks = taskRows.Count;
        var evidenceCount = taskRows.Sum(x => x.evidence_count);

        DateOnly? weekStartDate = taskRows.Count > 0 ? taskRows.Min(x => x.task_date) : null;
        DateOnly? weekEndDate = taskRows.Count > 0 ? taskRows.Max(x => x.task_date) : null;

        var reportTitle = string.IsNullOrWhiteSpace(request.report_title)
            ? $"Weekly Report - Week {request.week_no}"
            : request.report_title.Trim();

        var reportSummary = string.IsNullOrWhiteSpace(request.report_summary)
            ? $"Auto-generated from daily tasks. Total tasks: {totalTasks}, total evidences: {evidenceCount}."
            : request.report_summary.Trim();

        await using var tx = await _context.Database.BeginTransactionAsync();

        var weeklyReportId = _context.Database.SqlQuery<long>($"""
            INSERT INTO weekly_reports
                (
                    internship_id,
                    training_plan_id,
                    student_user_id,
                    week_no,
                    week_start_date,
                    week_end_date,
                    report_title,
                    report_summary,
                    total_tasks,
                    evidence_count,
                    generated_from_tasks,
                    generated_at,
                    approval_owner_user_id,
                    approval_owner_role,
                    assigned_advisor_user_id,
                    status,
                    created_at,
                    updated_at
                )
            VALUES
                (
                    {request.internship_id},
                    {internship.training_plan_id},
                    {internship.student_user_id},
                    {request.week_no},
                    {weekStartDate},
                    {weekEndDate},
                    {reportTitle},
                    {reportSummary},
                    {totalTasks},
                    {evidenceCount},
                    TRUE,
                    NOW(),
                    {internship.approval_owner_user_id},
                    {"AcademicAdvisor"}::approver_type_enum,
                    {internship.assigned_advisor_user_id},
                    {"Pending"}::approval_status_enum,
                    NOW(),
                    NOW()
                )
            RETURNING id AS "Value"
            """).AsEnumerable().Single();

        foreach (var task in taskRows)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO weekly_report_items
                    (
                        weekly_report_id,
                        task_id
                    )
                VALUES
                    (
                        {weeklyReportId},
                        {task.task_id}
                    )
                ON CONFLICT (weekly_report_id, task_id) DO NOTHING
                """);
        }

        await WorkflowWriteHelper.QueueInAppAsync(
            _context,
            internship.approval_owner_user_id,
            "Weekly Report Approval Request",
            $"A weekly report for week {request.week_no} has been generated and requires your review.",
            "weekly_reports",
            weeklyReportId
        );

        await tx.CommitAsync();

        return Ok(new
        {
            id = weeklyReportId,
            total_tasks = totalTasks,
            evidence_count = evidenceCount,
            message = "Weekly report generated successfully."
        });
    }

    [HttpGet("{id:long}/details")]
    public async Task<IActionResult> GetDetails(long id)
    {
        var report = await _context.Database.SqlQueryRaw<WeeklyReportDetailsDto>(
                """
                SELECT
                    wr.id,
                    wr.internship_id,
                    wr.student_user_id,
                    u.full_name AS student_name,
                    wr.week_no,
                    wr.report_title,
                    wr.report_summary,
                    wr.total_tasks,
                    wr.evidence_count,
                    wr.generated_at,
                    wr.status::text AS status,
                    owner.full_name AS approval_owner_name
                FROM weekly_reports wr
                JOIN users u ON u.id = wr.student_user_id
                JOIN users owner ON owner.id = wr.approval_owner_user_id
                WHERE wr.id = @id
                LIMIT 1
                """,
                new Npgsql.NpgsqlParameter("id", id))
            .FirstOrDefaultAsync();

        if (report == null)
            return NotFound(new { message = "Weekly report not found." });

        var items = await _context.Database.SqlQueryRaw<WeeklyReportTaskDetailsDto>(
                """
                SELECT
                    t.id AS task_id,
                    t.task_date,
                    t.week_no,
                    t.task_title,
                    COALESCE(ev.file_name, '') AS file_name,
                    ev.file_url
                FROM weekly_report_items wri
                JOIN training_tasks t ON t.id = wri.task_id
                LEFT JOIN training_task_evidences ev ON ev.task_id = t.id
                WHERE wri.weekly_report_id = @id
                ORDER BY t.task_date, t.id
                """,
                new Npgsql.NpgsqlParameter("id", id))
            .ToListAsync();

        return Ok(new
        {
            report,
            items
        });
    }

        [HttpPost("{id:long}/approve")]
        public async Task<IActionResult> Approve(long id, ReviewDecisionRequest request)
        {
            var item = await _context.weekly_reports
                .AsNoTracking()
                .Where(x => x.id == id)
                .Select(x => new { x.id, x.student_user_id })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = "Weekly report not found." });

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE weekly_reports
                SET status = {"Approved"}::approval_status_enum,
                    approval_comment = {request.comment},
                    reviewer_user_id = {request.actor_user_id},
                    reviewed_at = NOW(),
                    approved_at = NOW(),
                    updated_at = NOW()
                WHERE id = {id};
                """);

            await WorkflowWriteHelper.LogApprovalActionAsync(_context, "WeeklyReport", id, item.student_user_id, request.actor_user_id, "Approved", request.comment);
            await WorkflowWriteHelper.QueueInAppAsync(_context, item.student_user_id, "Weekly Report Approved", "Your weekly report was approved.", "weekly_reports", id);

            return Ok(new { message = "Weekly report approved successfully." });
        }

        [HttpPost("{id:long}/reject")]
        public async Task<IActionResult> Reject(long id, ReviewDecisionRequest request)
        {
            var item = await _context.weekly_reports
                .AsNoTracking()
                .Where(x => x.id == id)
                .Select(x => new { x.id, x.student_user_id })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = "Weekly report not found." });

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE weekly_reports
                SET status = {"Rejected"}::approval_status_enum,
                    approval_comment = {request.comment},
                    reviewer_user_id = {request.actor_user_id},
                    reviewed_at = NOW(),
                    rejected_at = NOW(),
                    updated_at = NOW()
                WHERE id = {id};
                """);

            await WorkflowWriteHelper.LogApprovalActionAsync(_context, "WeeklyReport", id, item.student_user_id, request.actor_user_id, "Rejected", request.comment);
            await WorkflowWriteHelper.QueueInAppAsync(_context, item.student_user_id, "Weekly Report Rejected", request.comment ?? "The weekly report was rejected.", "weekly_reports", id);

            return Ok(new { message = "Weekly report rejected successfully." });
        }
        public  class WeeklyReportInternshipLookupDto
        {
            public long internship_id { get; set; }
            public long student_user_id { get; set; }
            public long training_plan_id { get; set; }
            public long approval_owner_user_id { get; set; }
            public long assigned_advisor_user_id { get; set; }
        }

        public  class WeeklyReportTaskRowDto
        {
            public long task_id { get; set; }
            public DateOnly task_date { get; set; }
            public int evidence_count { get; set; }
        }

        private class WeeklyReportGenerateRow
        {
            public long id { get; set; }
            public long student_user_id { get; set; }
            public string provider_name { get; set; } = string.Empty;
            public string internship_title { get; set; } = string.Empty;
            public long training_plan_id { get; set; }
            public long approval_owner_user_id { get; set; }
            public string approval_owner_role { get; set; } = string.Empty;
            public long assigned_advisor_user_id { get; set; }
        }

        private class WeeklyTaskSourceRow
        {
            public long id { get; set; }
            public DateOnly task_date { get; set; }
            public int week_no { get; set; }
            public string task_title { get; set; } = string.Empty;
            public int evidence_count { get; set; }
        }
        public class WeeklyReportDetailsDto
        {
            public long id { get; set; }
            public long internship_id { get; set; }
            public long student_user_id { get; set; }
            public string student_name { get; set; } = string.Empty;
            public int week_no { get; set; }
            public string report_title { get; set; } = string.Empty;
            public string report_summary { get; set; } = string.Empty;
            public int total_tasks { get; set; }
            public int evidence_count { get; set; }
            public DateTime? generated_at { get; set; }
            public string status { get; set; } = string.Empty;
            public string approval_owner_name { get; set; } = string.Empty;
        }

        public class WeeklyReportTaskDetailsDto
        {
            public long task_id { get; set; }
            public DateOnly task_date { get; set; }
            public int week_no { get; set; }
            public string task_title { get; set; } = string.Empty;
            public string file_name { get; set; } = string.Empty;
            public string? file_url { get; set; }
        }
    }
}