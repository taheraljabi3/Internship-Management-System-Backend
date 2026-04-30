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
    [Authorize(Roles = "Student")]
    public class AttendanceController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AttendanceController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("me/today")]
        public async Task<IActionResult> GetMyTodayStatus()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            var internshipId = await ResolveInternshipId(userId.Value);
            if (!internshipId.HasValue)
                return Ok(new
                {
                    internship_id = (long?)null,
                    has_internship = false,
                    can_check_in = false,
                    can_check_out = false,
                    today_status = "No Internship",
                    today_entry = (object?)null
                });

            var todayRow = await _context.Database
                .SqlQueryRaw<AttendanceTodayRow>(
                    """
                    SELECT
                        id,
                        internship_id,
                        student_user_id,
                        entry_date,
                        check_in_time,
                        check_out_time,
                        daily_hours,
                        status::text AS status,
                        notes,
                        created_at,
                        updated_at
                    FROM attendance_entries
                    WHERE internship_id = @internshipId
                      AND student_user_id = @studentUserId
                      AND entry_date = timezone('Asia/Riyadh', now())::date
                    LIMIT 1
                    """,
                    new NpgsqlParameter("internshipId", internshipId.Value),
                    new NpgsqlParameter("studentUserId", userId.Value))
                .FirstOrDefaultAsync();

            var canCheckIn = todayRow == null || todayRow.check_out_time != null;
            var canCheckOut = todayRow != null && todayRow.check_in_time != null && todayRow.check_out_time == null;

            return Ok(new
            {
                internship_id = internshipId.Value,
                has_internship = true,
                can_check_in = canCheckIn,
                can_check_out = canCheckOut,
                today_status = todayRow == null
                    ? "Pending Check In"
                    : todayRow.check_out_time == null
                        ? "Checked In"
                        : "Checked Out",
                today_entry = todayRow
            });
        }

        [HttpGet("me/summary")]
        public async Task<IActionResult> GetMySummary()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            var internshipId = await ResolveInternshipId(userId.Value);
            if (!internshipId.HasValue)
                return Ok(new
                {
                    internship_id = (long?)null,
                    provider_name = "-",
                    internship_title = "-",
                    total_hours = 0,
                    present_days = 0,
                    absent_days = 0,
                    last_attendance_date = (DateTime?)null
                });

            var summary = await _context.Database
                .SqlQueryRaw<AttendanceSummaryRow>(
                    """
                    SELECT
                        internship_id,
                        student_user_id,
                        student_name,
                        provider_name,
                        internship_title,
                        total_hours,
                        present_days,
                        absent_days,
                        last_attendance_date
                    FROM vw_attendance_summary
                    WHERE internship_id = @internshipId
                    LIMIT 1
                    """,
                    new NpgsqlParameter("internshipId", internshipId.Value))
                .FirstOrDefaultAsync();

            if (summary == null)
            {
                return Ok(new
                {
                    internship_id = internshipId.Value,
                    provider_name = "-",
                    internship_title = "-",
                    total_hours = 0,
                    present_days = 0,
                    absent_days = 0,
                    last_attendance_date = (DateTime?)null
                });
            }

            return Ok(summary);
        }

        [HttpPost("me/check-in")]
        public async Task<IActionResult> CheckIn([FromBody] AttendanceCheckInRequest? request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            var internshipId = await ResolveInternshipId(userId.Value);
            if (!internshipId.HasValue)
                return BadRequest(new { message = "No approved internship context was found for this student." });

            var existing = await _context.Database
                .SqlQueryRaw<long>(
                    """
                    SELECT id AS "Value"
                    FROM attendance_entries
                    WHERE internship_id = @internshipId
                      AND student_user_id = @studentUserId
                      AND entry_date = timezone('Asia/Riyadh', now())::date
                    LIMIT 1
                    """,
                    new NpgsqlParameter("internshipId", internshipId.Value),
                    new NpgsqlParameter("studentUserId", userId.Value))
                .FirstOrDefaultAsync();

            if (existing > 0)
            {
                var today = await _context.Database
                    .SqlQueryRaw<AttendanceTodayRow>(
                        """
                        SELECT
                            id,
                            internship_id,
                            student_user_id,
                            entry_date,
                            check_in_time,
                            check_out_time,
                            daily_hours,
                            status::text AS status,
                            notes,
                            created_at,
                            updated_at
                        FROM attendance_entries
                        WHERE id = @id
                        LIMIT 1
                        """,
                        new NpgsqlParameter("id", existing))
                    .FirstOrDefaultAsync();

                if (today?.check_out_time == null)
                    return BadRequest(new { message = "The student has already checked in today." });

                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE attendance_entries
                    SET check_in_time = timezone('Asia/Riyadh', now())::time,
                        check_out_time = NULL,
                        daily_hours = COALESCE(daily_hours, 0),
                        status = {"Present"}::attendance_status_enum,
                        notes = CASE
                            WHEN {request?.notes} IS NULL OR {request?.notes} = '' THEN notes
                            WHEN notes IS NULL OR notes = '' THEN {request?.notes}
                            ELSE notes || E'\n' || {request?.notes}
                        END,
                        updated_at = NOW()
                    WHERE id = {existing}
                    """);
            }
            else
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO attendance_entries
                        (
                            internship_id,
                            student_user_id,
                            entry_date,
                            check_in_time,
                            check_out_time,
                            daily_hours,
                            status,
                            notes,
                            created_by_user_id,
                            created_at,
                            updated_at
                        )
                    VALUES
                        (
                            {internshipId.Value},
                            {userId.Value},
                            timezone('Asia/Riyadh', now())::date,
                            timezone('Asia/Riyadh', now())::time,
                            NULL,
                            0,
                            {"Present"}::attendance_status_enum,
                            {request?.notes},
                            {userId.Value},
                            NOW(),
                            NOW()
                        )
                    """);
            }

            return Ok(new { message = "Checked in successfully." });
        }

        [HttpPost("me/check-out")]
        public async Task<IActionResult> CheckOut([FromBody] AttendanceCheckOutRequest? request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            var internshipId = await ResolveInternshipId(userId.Value);
            if (!internshipId.HasValue)
                return BadRequest(new { message = "No approved internship context was found for this student." });

            var today = await _context.Database
                .SqlQueryRaw<AttendanceTodayRow>(
                    """
                    SELECT
                        id,
                        internship_id,
                        student_user_id,
                        entry_date,
                        check_in_time,
                        check_out_time,
                        daily_hours,
                        status::text AS status,
                        notes,
                        created_at,
                        updated_at
                    FROM attendance_entries
                    WHERE internship_id = @internshipId
                      AND student_user_id = @studentUserId
                      AND entry_date = timezone('Asia/Riyadh', now())::date
                    LIMIT 1
                    """,
                    new NpgsqlParameter("internshipId", internshipId.Value),
                    new NpgsqlParameter("studentUserId", userId.Value))
                .FirstOrDefaultAsync();

            if (today == null || today.check_in_time == null)
                return BadRequest(new { message = "The student must check in first." });

            if (today.check_out_time != null)
                return BadRequest(new { message = "The student has already checked out today." });

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE attendance_entries
                SET check_out_time = timezone('Asia/Riyadh', now())::time,
                    daily_hours = COALESCE(daily_hours, 0) + ROUND(
                        EXTRACT(
                            EPOCH FROM (
                                timezone('Asia/Riyadh', now())::time - check_in_time
                            )
                        ) / 3600.0
                    , 2),
                    notes = CASE
                        WHEN {request?.notes} IS NULL OR {request?.notes} = '' THEN notes
                        WHEN notes IS NULL OR notes = '' THEN {request?.notes}
                        ELSE notes || E'\n' || {request?.notes}
                    END,
                    updated_at = NOW()
                WHERE id = {today.id}
                """);

            return Ok(new { message = "Checked out successfully." });
        }

        [HttpGet("me/history")]
        public async Task<IActionResult> GetMyHistory()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            var internshipId = await ResolveInternshipId(userId.Value);
            if (!internshipId.HasValue)
                return Ok(Array.Empty<object>());

            var rows = await _context.Database
                .SqlQueryRaw<AttendanceTodayRow>(
                    """
                    SELECT
                        id,
                        internship_id,
                        student_user_id,
                        entry_date,
                        check_in_time,
                        check_out_time,
                        daily_hours,
                        status::text AS status,
                        notes,
                        created_at,
                        updated_at
                    FROM attendance_entries
                    WHERE internship_id = @internshipId
                      AND student_user_id = @studentUserId
                    ORDER BY entry_date DESC, id DESC
                    """,
                    new NpgsqlParameter("internshipId", internshipId.Value),
                    new NpgsqlParameter("studentUserId", userId.Value))
                .ToListAsync();

            return Ok(rows);
        }

        private async Task<long?> ResolveInternshipId(long studentUserId)
        {
            var internshipId = await _context.Database
                .SqlQueryRaw<long>(
                    """
                    SELECT id AS "Value"
                    FROM internships
                    WHERE student_user_id = @studentUserId
                    ORDER BY id DESC
                    LIMIT 1
                    """,
                    new NpgsqlParameter("studentUserId", studentUserId))
                .FirstOrDefaultAsync();

            return internshipId > 0 ? internshipId : null;
        }

        private long? GetCurrentUserId()
        {
            var raw =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub");

            return long.TryParse(raw, out var id) ? id : null;
        }
    }

    public class AttendanceCheckInRequest
    {
        public string? notes { get; set; }
    }

    public class AttendanceCheckOutRequest
    {
        public string? notes { get; set; }
    }

    public class AttendanceTodayRow
    {
        public long id { get; set; }
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public DateTime entry_date { get; set; }
        public TimeSpan? check_in_time { get; set; }
        public TimeSpan? check_out_time { get; set; }
        public decimal daily_hours { get; set; }
        public string status { get; set; } = string.Empty;
        public string? notes { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class AttendanceSummaryRow
    {
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public string student_name { get; set; } = string.Empty;
        public string provider_name { get; set; } = string.Empty;
        public string internship_title { get; set; } = string.Empty;
        public decimal total_hours { get; set; }
        public int present_days { get; set; }
        public int absent_days { get; set; }
        public DateTime? last_attendance_date { get; set; }
    }
}