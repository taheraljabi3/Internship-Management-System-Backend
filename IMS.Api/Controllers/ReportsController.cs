using System.Security.Claims;
using IMS.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator,AcademicAdvisor")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // Admin Reports
        // ============================================================

        [HttpGet("admin/overview")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAdminOverview()
        {
            var overview = await _context.Database
                .SqlQueryRaw<AdminOverviewReportDto>(
                    """
                    SELECT
                        (SELECT COUNT(*) FROM student_profiles)::int AS total_students,
                        (SELECT COUNT(*) FROM internships)::int AS active_internships,
                        (SELECT COUNT(*) FROM training_company_requests WHERE status::text = 'Pending')::int AS pending_training_requests,
                        (SELECT COUNT(*) FROM training_plans WHERE status::text IN ('Approved', 'Completed'))::int AS completed_training_plans,
                        (SELECT COUNT(*) FROM training_plans)::int AS training_plans_count,
                        (SELECT COUNT(*) FROM weekly_reports)::int AS submitted_weekly_reports,
                        (SELECT COUNT(*) FROM company_evaluations)::int AS company_evaluations_submitted,
                        (SELECT COUNT(*) FROM academic_evaluations)::int AS academic_evaluations_submitted,
                        COALESCE((
                            SELECT ROUND(
                                100.0 * COUNT(*) FILTER (WHERE status::text = 'Present')
                                / NULLIF(COUNT(*) FILTER (WHERE status::text IN ('Present', 'Absent')), 0),
                                2
                            )
                            FROM attendance_entries
                        ), 0) AS attendance_rate
                    """)
                .FirstOrDefaultAsync();

            return Ok(overview ?? new AdminOverviewReportDto());
        }

        [HttpGet("admin/attendance-trend")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAdminAttendanceTrend([FromQuery] int days = 30)
        {
            days = NormalizeDays(days);

            var rows = await _context.Database
                .SqlQueryRaw<AttendanceTrendPointDto>(
                    """
                    WITH date_range AS (
                        SELECT generate_series(
                            timezone('Asia/Riyadh', now())::date - CAST(@days AS integer),
                            timezone('Asia/Riyadh', now())::date,
                            interval '1 day'
                        )::date AS report_date
                    )
                    SELECT
                        dr.report_date,
                        dr.report_date::text AS period,
                        COUNT(a.id) FILTER (WHERE a.status::text = 'Present')::int AS present_count,
                        COUNT(a.id) FILTER (WHERE a.status::text = 'Present')::int AS present,
                        COUNT(a.id) FILTER (WHERE a.status::text = 'Absent')::int AS absent_count,
                        COUNT(a.id) FILTER (WHERE a.status::text = 'Absent')::int AS absent,
                        COALESCE(ROUND(COALESCE(SUM(a.daily_hours), 0)::numeric, 2), 0) AS total_hours
                    FROM date_range dr
                    LEFT JOIN attendance_entries a
                        ON a.entry_date = dr.report_date
                    GROUP BY dr.report_date
                    ORDER BY dr.report_date
                    """,
                    new NpgsqlParameter("days", days))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpGet("admin/evaluation-distribution")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAdminEvaluationDistribution()
        {
            var rows = await _context.Database
                .SqlQueryRaw<EvaluationDistributionDto>(
                    """
                    WITH scores AS (
                        SELECT total_percentage FROM company_evaluations WHERE total_percentage IS NOT NULL
                        UNION ALL
                        SELECT total_percentage FROM academic_evaluations WHERE total_percentage IS NOT NULL
                    ),
                    bucketed AS (
                        SELECT
                            CASE
                                WHEN total_percentage >= 90 THEN '90-100'
                                WHEN total_percentage >= 80 THEN '80-89'
                                WHEN total_percentage >= 70 THEN '70-79'
                                WHEN total_percentage >= 60 THEN '60-69'
                                ELSE 'Below 60'
                            END AS score_range,
                            CASE
                                WHEN total_percentage >= 90 THEN 5
                                WHEN total_percentage >= 80 THEN 4
                                WHEN total_percentage >= 70 THEN 3
                                WHEN total_percentage >= 60 THEN 2
                                ELSE 1
                            END AS sort_order
                        FROM scores
                    )
                    SELECT
                        score_range,
                        score_range AS name,
                        COUNT(*)::int AS evaluations_count,
                        COUNT(*)::int AS value,
                        COUNT(*)::int AS total,
                        sort_order
                    FROM bucketed
                    GROUP BY score_range, sort_order
                    ORDER BY sort_order DESC
                    """)
                .ToListAsync();

            return Ok(rows);
        }

        [HttpGet("admin/advisor-workload")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAdminAdvisorWorkload()
        {
            var rows = await _context.Database
                .SqlQueryRaw<AdvisorWorkloadReportDto>(
                    """
                    SELECT
                        adv.advisor_user_id,
                        adv.advisor_name,
                        adv.advisor_email,
                        COUNT(DISTINCT adv.student_user_id)::int AS assigned_students,
                        COUNT(DISTINCT i.id)::int AS active_internships,
                        COUNT(DISTINCT ae.id)::int AS academic_evaluations_submitted,
                        COUNT(DISTINCT wr.id)::int AS weekly_reports_count
                    FROM vw_student_current_advisors adv
                    LEFT JOIN internships i
                        ON i.student_user_id = adv.student_user_id
                    LEFT JOIN academic_evaluations ae
                        ON ae.student_user_id = adv.student_user_id
                    LEFT JOIN weekly_reports wr
                        ON wr.internship_id = i.id
                    GROUP BY
                        adv.advisor_user_id,
                        adv.advisor_name,
                        adv.advisor_email
                    ORDER BY assigned_students DESC, adv.advisor_name
                    """)
                .ToListAsync();

            return Ok(rows);
        }

        [HttpGet("admin/provider-performance")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetAdminProviderPerformance()
        {
            var rows = await _context.Database
                .SqlQueryRaw<ProviderPerformanceReportDto>(
                    """
                    SELECT
                        COALESCE(i.provider_name, '-') AS provider_name,
                        COUNT(DISTINCT i.id)::int AS internships_count,
                        COUNT(DISTINCT i.id)::int AS active_internships,
                        COUNT(DISTINCT i.student_user_id)::int AS students_count,
                        COALESCE(ROUND(AVG(ce.total_percentage)::numeric, 2), 0) AS average_company_score,
                        COALESCE(ROUND(AVG(ce.total_percentage)::numeric, 2), 0) AS avg_company_score,
                        COALESCE(ROUND(COALESCE(SUM(a.daily_hours), 0)::numeric, 2), 0) AS total_attendance_hours,
                        COUNT(a.id) FILTER (WHERE a.status::text = 'Present')::int AS present_days,
                        COUNT(a.id) FILTER (WHERE a.status::text = 'Absent')::int AS absent_days,
                        COALESCE(ROUND(
                            100.0 * COUNT(a.id) FILTER (WHERE a.status::text = 'Present')
                            / NULLIF(COUNT(a.id) FILTER (WHERE a.status::text IN ('Present', 'Absent')), 0),
                            2
                        ), 0) AS attendance_rate
                    FROM internships i
                    LEFT JOIN company_evaluations ce
                        ON ce.internship_id = i.id
                    LEFT JOIN attendance_entries a
                        ON a.internship_id = i.id
                    GROUP BY COALESCE(i.provider_name, '-')
                    ORDER BY internships_count DESC, average_company_score DESC
                    """)
                .ToListAsync();

            return Ok(rows);
        }

        // ============================================================
        // Academic Advisor Reports
        // ============================================================

        [HttpGet("advisor/{advisorUserId:long}/overview")]
        [Authorize(Roles = "Administrator,AcademicAdvisor")]
        public async Task<IActionResult> GetAdvisorOverview(long advisorUserId)
        {
            var accessError = EnsureAdvisorAccess(advisorUserId);
            if (accessError != null)
                return accessError;

            var overview = await _context.Database
                .SqlQueryRaw<AdvisorOverviewReportDto>(
                    """
                    WITH assigned_students AS (
                        SELECT student_user_id
                        FROM vw_student_current_advisors
                        WHERE advisor_user_id = @advisorUserId
                    ),
                    assigned_internships AS (
                        SELECT
                            i.id,
                            i.student_user_id,
                            i.provider_name
                        FROM internships i
                        JOIN assigned_students s
                            ON s.student_user_id = i.student_user_id
                    ),
                    latest_weekly_reports AS (
                        SELECT
                            i.id AS internship_id,
                            MAX(wr.created_at) AS last_report_at,
                            BOOL_OR(wr.status::text = 'Pending') AS has_pending_report
                        FROM assigned_internships i
                        LEFT JOIN weekly_reports wr
                            ON wr.internship_id = i.id
                        GROUP BY i.id
                    ),
                    scores AS (
                        SELECT ce.total_percentage AS score
                        FROM company_evaluations ce
                        JOIN assigned_internships i
                            ON i.id = ce.internship_id
                        WHERE ce.total_percentage IS NOT NULL
                        UNION ALL
                        SELECT ae.total_percentage AS score
                        FROM academic_evaluations ae
                        JOIN assigned_internships i
                            ON i.id = ae.internship_id
                        WHERE ae.total_percentage IS NOT NULL
                    )
                    SELECT
                        (SELECT COUNT(*) FROM assigned_students)::int AS assigned_students,
                        (SELECT COUNT(*) FROM assigned_students)::int AS assigned_students_count,
                        (SELECT COUNT(*) FROM assigned_internships)::int AS active_internships,
                        (SELECT COUNT(*) FROM assigned_internships)::int AS active_internships_count,
                        (
                            SELECT COUNT(*)
                            FROM assigned_internships i
                            WHERE NOT EXISTS (
                                SELECT 1
                                FROM attendance_entries a
                                WHERE a.internship_id = i.id
                                  AND a.entry_date >= timezone('Asia/Riyadh', now())::date - 7
                            )
                        )::int AS missing_attendance,
                        (
                            SELECT COUNT(*)
                            FROM assigned_internships i
                            WHERE NOT EXISTS (
                                SELECT 1
                                FROM attendance_entries a
                                WHERE a.internship_id = i.id
                                  AND a.entry_date = timezone('Asia/Riyadh', now())::date
                            )
                        )::int AS students_missing_attendance_today,
                        (
                            SELECT COUNT(*)
                            FROM latest_weekly_reports wr
                            WHERE COALESCE(wr.has_pending_report, false) = true
                               OR wr.last_report_at IS NULL
                               OR wr.last_report_at < timezone('Asia/Riyadh', now()) - interval '7 days'
                        )::int AS pending_weekly_reports,
                        (
                            SELECT COUNT(*)
                            FROM assigned_internships i
                            WHERE NOT EXISTS (
                                SELECT 1
                                FROM academic_evaluations ae
                                WHERE ae.internship_id = i.id
                            )
                        )::int AS pending_academic_evaluations,
                        (
                            SELECT COUNT(DISTINCT ce.internship_id)
                            FROM company_evaluations ce
                            JOIN assigned_internships i
                                ON i.id = ce.internship_id
                        )::int AS company_evaluations_received,
                        COALESCE((SELECT ROUND(AVG(score)::numeric, 2) FROM scores), 0) AS average_student_performance
                    """,
                    new NpgsqlParameter("advisorUserId", advisorUserId))
                .FirstOrDefaultAsync();

            return Ok(overview ?? new AdvisorOverviewReportDto());
        }

        [HttpGet("advisor/{advisorUserId:long}/attendance-trend")]
        [Authorize(Roles = "Administrator,AcademicAdvisor")]
        public async Task<IActionResult> GetAdvisorAttendanceTrend(long advisorUserId, [FromQuery] int days = 30)
        {
            var accessError = EnsureAdvisorAccess(advisorUserId);
            if (accessError != null)
                return accessError;

            days = NormalizeDays(days);

            var rows = await _context.Database
                .SqlQueryRaw<AttendanceTrendPointDto>(
                    """
                    WITH date_range AS (
                        SELECT generate_series(
                            timezone('Asia/Riyadh', now())::date - CAST(@days AS integer),
                            timezone('Asia/Riyadh', now())::date,
                            interval '1 day'
                        )::date AS report_date
                    ),
                    assigned_students AS (
                        SELECT student_user_id
                        FROM vw_student_current_advisors
                        WHERE advisor_user_id = @advisorUserId
                    )
                    SELECT
                        dr.report_date,
                        dr.report_date::text AS period,
                        COUNT(a.id) FILTER (WHERE a.status::text = 'Present')::int AS present_count,
                        COUNT(a.id) FILTER (WHERE a.status::text = 'Present')::int AS present,
                        COUNT(a.id) FILTER (WHERE a.status::text = 'Absent')::int AS absent_count,
                        COUNT(a.id) FILTER (WHERE a.status::text = 'Absent')::int AS absent,
                        COALESCE(ROUND(COALESCE(SUM(a.daily_hours), 0)::numeric, 2), 0) AS total_hours
                    FROM date_range dr
                    LEFT JOIN attendance_entries a
                        ON a.entry_date = dr.report_date
                       AND a.student_user_id IN (SELECT student_user_id FROM assigned_students)
                    GROUP BY dr.report_date
                    ORDER BY dr.report_date
                    """,
                    new NpgsqlParameter("days", days),
                    new NpgsqlParameter("advisorUserId", advisorUserId))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpGet("advisor/{advisorUserId:long}/students-performance")]
        [Authorize(Roles = "Administrator,AcademicAdvisor")]
        public async Task<IActionResult> GetAdvisorStudentsPerformance(long advisorUserId)
        {
            var accessError = EnsureAdvisorAccess(advisorUserId);
            if (accessError != null)
                return accessError;

            var rows = await _context.Database
                .SqlQueryRaw<StudentPerformanceReportDto>(
                    """
                    WITH assigned_students AS (
                        SELECT
                            adv.student_user_id,
                            u.full_name AS student_name,
                            u.email AS student_email
                        FROM vw_student_current_advisors adv
                        JOIN users u
                            ON u.id = adv.student_user_id
                        WHERE adv.advisor_user_id = @advisorUserId
                    ),
                    latest_internships AS (
                        SELECT DISTINCT ON (i.student_user_id)
                            i.id AS internship_id,
                            i.student_user_id,
                            COALESCE(i.provider_name, '-') AS provider_name,
                            '-' AS internship_title
                        FROM internships i
                        JOIN assigned_students s
                            ON s.student_user_id = i.student_user_id
                        ORDER BY i.student_user_id, i.id DESC
                    ),
                    attendance AS (
                        SELECT
                            a.student_user_id,
                            COALESCE(ROUND(COALESCE(SUM(a.daily_hours), 0)::numeric, 2), 0) AS total_hours,
                            COUNT(a.id) FILTER (WHERE a.status::text = 'Present')::int AS present_days,
                            COUNT(a.id) FILTER (WHERE a.status::text = 'Absent')::int AS absent_days,
                            MAX(a.entry_date) AS last_attendance_date
                        FROM attendance_entries a
                        JOIN assigned_students s
                            ON s.student_user_id = a.student_user_id
                        GROUP BY a.student_user_id
                    ),
                    company_scores AS (
                        SELECT
                            ce.student_user_id,
                            ROUND(AVG(ce.total_percentage)::numeric, 2) AS company_score
                        FROM company_evaluations ce
                        JOIN assigned_students s
                            ON s.student_user_id = ce.student_user_id
                        GROUP BY ce.student_user_id
                    ),
                    academic_scores AS (
                        SELECT
                            ae.student_user_id,
                            ROUND(AVG(ae.total_percentage)::numeric, 2) AS academic_score
                        FROM academic_evaluations ae
                        JOIN assigned_students s
                            ON s.student_user_id = ae.student_user_id
                        GROUP BY ae.student_user_id
                    )
                    SELECT
                        s.student_user_id,
                        s.student_name,
                        s.student_email,
                        COALESCE(li.internship_id, 0) AS internship_id,
                        COALESCE(li.provider_name, '-') AS provider_name,
                        COALESCE(li.internship_title, '-') AS internship_title,
                        COALESCE(att.total_hours, 0) AS total_hours,
                        COALESCE(att.present_days, 0) AS present_days,
                        COALESCE(att.absent_days, 0) AS absent_days,
                        att.last_attendance_date,
                        COALESCE(cs.company_score, 0) AS company_score,
                        COALESCE(cs.company_score, 0) AS company_total_percentage,
                        COALESCE(acs.academic_score, 0) AS academic_score,
                        COALESCE(acs.academic_score, 0) AS academic_total_percentage,
                        COALESCE(ROUND(((
                            NULLIF(COALESCE(cs.company_score, 0), 0) +
                            NULLIF(COALESCE(acs.academic_score, 0), 0)
                        ) / NULLIF(
                            (CASE WHEN COALESCE(cs.company_score, 0) > 0 THEN 1 ELSE 0 END) +
                            (CASE WHEN COALESCE(acs.academic_score, 0) > 0 THEN 1 ELSE 0 END),
                            0
                        ))::numeric, 2), 0) AS overall_score,
                        COALESCE(ROUND(((
                            NULLIF(COALESCE(cs.company_score, 0), 0) +
                            NULLIF(COALESCE(acs.academic_score, 0), 0)
                        ) / NULLIF(
                            (CASE WHEN COALESCE(cs.company_score, 0) > 0 THEN 1 ELSE 0 END) +
                            (CASE WHEN COALESCE(acs.academic_score, 0) > 0 THEN 1 ELSE 0 END),
                            0
                        ))::numeric, 2), 0) AS final_average
                    FROM assigned_students s
                    LEFT JOIN latest_internships li
                        ON li.student_user_id = s.student_user_id
                    LEFT JOIN attendance att
                        ON att.student_user_id = s.student_user_id
                    LEFT JOIN company_scores cs
                        ON cs.student_user_id = s.student_user_id
                    LEFT JOIN academic_scores acs
                        ON acs.student_user_id = s.student_user_id
                    ORDER BY overall_score DESC, s.student_name
                    """,
                    new NpgsqlParameter("advisorUserId", advisorUserId))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpGet("advisor/{advisorUserId:long}/evaluation-status")]
        [Authorize(Roles = "Administrator,AcademicAdvisor")]
        public async Task<IActionResult> GetAdvisorEvaluationStatus(long advisorUserId)
        {
            var accessError = EnsureAdvisorAccess(advisorUserId);
            if (accessError != null)
                return accessError;

            var rows = await _context.Database
                .SqlQueryRaw<ReportNameValueDto>(
                    """
                    WITH assigned_students AS (
                        SELECT student_user_id
                        FROM vw_student_current_advisors
                        WHERE advisor_user_id = @advisorUserId
                    ),
                    assigned_internships AS (
                        SELECT i.id, i.student_user_id
                        FROM internships i
                        JOIN assigned_students s
                            ON s.student_user_id = i.student_user_id
                    ),
                    counts AS (
                        SELECT
                            (SELECT COUNT(*) FROM assigned_internships)::int AS total_internships,
                            (
                                SELECT COUNT(DISTINCT ce.internship_id)
                                FROM company_evaluations ce
                                JOIN assigned_internships i
                                    ON i.id = ce.internship_id
                            )::int AS company_received,
                            (
                                SELECT COUNT(*)
                                FROM assigned_internships i
                                WHERE NOT EXISTS (
                                    SELECT 1
                                    FROM company_evaluations ce
                                    WHERE ce.internship_id = i.id
                                )
                            )::int AS company_pending,
                            (
                                SELECT COUNT(DISTINCT ae.internship_id)
                                FROM academic_evaluations ae
                                JOIN assigned_internships i
                                    ON i.id = ae.internship_id
                            )::int AS academic_submitted,
                            (
                                SELECT COUNT(*)
                                FROM assigned_internships i
                                WHERE NOT EXISTS (
                                    SELECT 1
                                    FROM academic_evaluations ae
                                    WHERE ae.internship_id = i.id
                                )
                            )::int AS academic_pending
                    )
                    SELECT 'Company Received' AS name, company_received AS value, 1 AS sort_order FROM counts
                    UNION ALL
                    SELECT 'Company Pending' AS name, company_pending AS value, 2 AS sort_order FROM counts
                    UNION ALL
                    SELECT 'Academic Submitted' AS name, academic_submitted AS value, 3 AS sort_order FROM counts
                    UNION ALL
                    SELECT 'Academic Pending' AS name, academic_pending AS value, 4 AS sort_order FROM counts
                    ORDER BY sort_order
                    """,
                    new NpgsqlParameter("advisorUserId", advisorUserId))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpGet("advisor/{advisorUserId:long}/risk-students")]
        [Authorize(Roles = "Administrator,AcademicAdvisor")]
        public async Task<IActionResult> GetAdvisorRiskStudents(long advisorUserId)
        {
            var accessError = EnsureAdvisorAccess(advisorUserId);
            if (accessError != null)
                return accessError;

            var rows = await _context.Database
                .SqlQueryRaw<RiskStudentReportDto>(
                    """
                    WITH assigned_students AS (
                        SELECT
                            adv.student_user_id,
                            u.full_name AS student_name,
                            u.email AS student_email
                        FROM vw_student_current_advisors adv
                        JOIN users u
                            ON u.id = adv.student_user_id
                        WHERE adv.advisor_user_id = @advisorUserId
                    ),
                    latest_internships AS (
                        SELECT DISTINCT ON (i.student_user_id)
                            i.id AS internship_id,
                            i.student_user_id,
                            COALESCE(i.provider_name, '-') AS provider_name,
                            '-' AS internship_title
                        FROM internships i
                        JOIN assigned_students s
                            ON s.student_user_id = i.student_user_id
                        ORDER BY i.student_user_id, i.id DESC
                    ),
                    attendance AS (
                        SELECT
                            a.student_user_id,
                            COALESCE(ROUND(COALESCE(SUM(a.daily_hours), 0)::numeric, 2), 0) AS total_hours,
                            COUNT(a.id) FILTER (WHERE a.status::text = 'Absent')::int AS absent_days,
                            MAX(a.entry_date) AS last_attendance_date
                        FROM attendance_entries a
                        JOIN assigned_students s
                            ON s.student_user_id = a.student_user_id
                        GROUP BY a.student_user_id
                    ),
                    reports AS (
                        SELECT
                            i.student_user_id,
                            MAX(wr.created_at) AS last_weekly_report_at
                        FROM internships i
                        JOIN assigned_students s
                            ON s.student_user_id = i.student_user_id
                        LEFT JOIN weekly_reports wr
                            ON wr.internship_id = i.id
                        GROUP BY i.student_user_id
                    )
                    SELECT
                        s.student_user_id,
                        s.student_name,
                        s.student_email,
                        COALESCE(li.provider_name, '-') AS provider_name,
                        COALESCE(li.internship_title, '-') AS internship_title,
                        COALESCE(a.total_hours, 0) AS total_hours,
                        COALESCE(a.absent_days, 0) AS absent_days,
                        a.last_attendance_date,
                        r.last_weekly_report_at,
                        CASE
                            WHEN COALESCE(a.absent_days, 0) >= 3 THEN 'High'
                            WHEN a.last_attendance_date IS NULL THEN 'Medium'
                            WHEN a.last_attendance_date < timezone('Asia/Riyadh', now())::date - 7 THEN 'Medium'
                            WHEN r.last_weekly_report_at IS NULL THEN 'Medium'
                            WHEN r.last_weekly_report_at < timezone('Asia/Riyadh', now()) - interval '7 days' THEN 'Medium'
                            ELSE 'Low'
                        END AS risk_level,
                        CASE
                            WHEN COALESCE(a.absent_days, 0) >= 3 THEN 'Frequent absences'
                            WHEN a.last_attendance_date IS NULL THEN 'No attendance records'
                            WHEN a.last_attendance_date < timezone('Asia/Riyadh', now())::date - 7 THEN 'No recent attendance'
                            WHEN r.last_weekly_report_at IS NULL THEN 'No weekly report yet'
                            WHEN r.last_weekly_report_at < timezone('Asia/Riyadh', now()) - interval '7 days' THEN 'No recent weekly report'
                            ELSE 'Normal'
                        END AS risk_reason
                    FROM assigned_students s
                    LEFT JOIN latest_internships li
                        ON li.student_user_id = s.student_user_id
                    LEFT JOIN attendance a
                        ON a.student_user_id = s.student_user_id
                    LEFT JOIN reports r
                        ON r.student_user_id = s.student_user_id
                    WHERE
                        COALESCE(a.absent_days, 0) >= 3
                        OR a.last_attendance_date IS NULL
                        OR a.last_attendance_date < timezone('Asia/Riyadh', now())::date - 7
                        OR r.last_weekly_report_at IS NULL
                        OR r.last_weekly_report_at < timezone('Asia/Riyadh', now()) - interval '7 days'
                    ORDER BY
                        CASE
                            WHEN COALESCE(a.absent_days, 0) >= 3 THEN 1
                            WHEN a.last_attendance_date IS NULL THEN 2
                            WHEN a.last_attendance_date < timezone('Asia/Riyadh', now())::date - 7 THEN 3
                            WHEN r.last_weekly_report_at IS NULL THEN 4
                            WHEN r.last_weekly_report_at < timezone('Asia/Riyadh', now()) - interval '7 days' THEN 5
                            ELSE 6
                        END,
                        s.student_name
                    """,
                    new NpgsqlParameter("advisorUserId", advisorUserId))
                .ToListAsync();

            return Ok(rows);
        }

        // ============================================================
        // Helpers
        // ============================================================

        private static int NormalizeDays(int days)
        {
            if (days < 7)
                return 7;

            if (days > 365)
                return 365;

            return days;
        }

        private IActionResult? EnsureAdvisorAccess(long advisorUserId)
        {
            if (IsAdministrator())
                return null;

            var currentUserId = GetCurrentUserId();

            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            if (currentUserId.Value != advisorUserId)
                return Forbid();

            return null;
        }

        private bool IsAdministrator()
        {
            return User.IsInRole("Administrator");
        }

        private long? GetCurrentUserId()
        {
            var raw =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub");

            return long.TryParse(raw, out var id) ? id : null;
        }
    }

    public class AdminOverviewReportDto
    {
        public int total_students { get; set; }
        public int active_internships { get; set; }
        public int pending_training_requests { get; set; }
        public int completed_training_plans { get; set; }
        public int training_plans_count { get; set; }
        public int submitted_weekly_reports { get; set; }
        public int company_evaluations_submitted { get; set; }
        public int academic_evaluations_submitted { get; set; }
        public decimal attendance_rate { get; set; }
    }

    public class AdvisorOverviewReportDto
    {
        public int assigned_students { get; set; }
        public int assigned_students_count { get; set; }
        public int active_internships { get; set; }
        public int active_internships_count { get; set; }
        public int missing_attendance { get; set; }
        public int students_missing_attendance_today { get; set; }
        public int pending_weekly_reports { get; set; }
        public int pending_academic_evaluations { get; set; }
        public int company_evaluations_received { get; set; }
        public decimal average_student_performance { get; set; }
    }

    public class AttendanceTrendPointDto
    {
        public DateTime report_date { get; set; }
        public string period { get; set; } = string.Empty;
        public int present_count { get; set; }
        public int present { get; set; }
        public int absent_count { get; set; }
        public int absent { get; set; }
        public decimal total_hours { get; set; }
    }

    public class EvaluationDistributionDto
    {
        public string score_range { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public int evaluations_count { get; set; }
        public int value { get; set; }
        public int total { get; set; }
        public int sort_order { get; set; }
    }

    public class ReportNameValueDto
    {
        public string name { get; set; } = string.Empty;
        public int value { get; set; }
        public int sort_order { get; set; }
    }

    public class AdvisorWorkloadReportDto
    {
        public long advisor_user_id { get; set; }
        public string advisor_name { get; set; } = string.Empty;
        public string advisor_email { get; set; } = string.Empty;
        public int assigned_students { get; set; }
        public int active_internships { get; set; }
        public int academic_evaluations_submitted { get; set; }
        public int weekly_reports_count { get; set; }
    }

    public class ProviderPerformanceReportDto
    {
        public string provider_name { get; set; } = string.Empty;
        public int internships_count { get; set; }
        public int active_internships { get; set; }
        public int students_count { get; set; }
        public decimal average_company_score { get; set; }
        public decimal avg_company_score { get; set; }
        public decimal total_attendance_hours { get; set; }
        public int present_days { get; set; }
        public int absent_days { get; set; }
        public decimal attendance_rate { get; set; }
    }

    public class StudentPerformanceReportDto
    {
        public long student_user_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string student_email { get; set; } = string.Empty;
        public long internship_id { get; set; }
        public string provider_name { get; set; } = string.Empty;
        public string internship_title { get; set; } = string.Empty;
        public decimal total_hours { get; set; }
        public int present_days { get; set; }
        public int absent_days { get; set; }
        public DateTime? last_attendance_date { get; set; }
        public decimal company_score { get; set; }
        public decimal company_total_percentage { get; set; }
        public decimal academic_score { get; set; }
        public decimal academic_total_percentage { get; set; }
        public decimal overall_score { get; set; }
        public decimal final_average { get; set; }
    }

    public class RiskStudentReportDto
    {
        public long student_user_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string student_email { get; set; } = string.Empty;
        public string provider_name { get; set; } = string.Empty;
        public string internship_title { get; set; } = string.Empty;
        public decimal total_hours { get; set; }
        public int absent_days { get; set; }
        public DateTime? last_attendance_date { get; set; }
        public DateTime? last_weekly_report_at { get; set; }
        public string risk_level { get; set; } = string.Empty;
        public string risk_reason { get; set; } = string.Empty;
    }
}