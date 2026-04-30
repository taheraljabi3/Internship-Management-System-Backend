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
    [Authorize]
    public class AcademicStudentEvaluationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AcademicStudentEvaluationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("internship/{internshipId:long}")]
        [Authorize(Roles = "AcademicAdvisor,Administrator,Student")]
        public async Task<IActionResult> GetByInternship(long internshipId)
        {
            var rows = await _context.Database
                .SqlQueryRaw<AcademicStudentEvaluationDto>(
                    """
                    SELECT
                        id,
                        internship_id,
                        student_user_id,
                        advisor_user_id,
                        evaluator_name,
                        evaluation_date,
                        status,
                        commitment_score,
                        communication_score,
                        technical_score,
                        behavior_score,
                        total_percentage,
                        strengths,
                        improvement_areas,
                        advisor_notes,
                        created_at,
                        updated_at
                    FROM academic_student_evaluations
                    WHERE internship_id = @internshipId
                    ORDER BY updated_at DESC, id DESC
                    """,
                    new NpgsqlParameter("internshipId", internshipId))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpGet("student/{studentUserId:long}")]
        [Authorize(Roles = "AcademicAdvisor,Administrator")]
        public async Task<IActionResult> GetByStudent(long studentUserId)
        {
            var rows = await _context.Database
                .SqlQueryRaw<AcademicStudentEvaluationDto>(
                    """
                    SELECT
                        id,
                        internship_id,
                        student_user_id,
                        advisor_user_id,
                        evaluator_name,
                        evaluation_date,
                        status,
                        commitment_score,
                        communication_score,
                        technical_score,
                        behavior_score,
                        total_percentage,
                        strengths,
                        improvement_areas,
                        advisor_notes,
                        created_at,
                        updated_at
                    FROM academic_student_evaluations
                    WHERE student_user_id = @studentUserId
                    ORDER BY updated_at DESC, id DESC
                    """,
                    new NpgsqlParameter("studentUserId", studentUserId))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpGet("me")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyEvaluations()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            var rows = await _context.Database
                .SqlQueryRaw<AcademicStudentEvaluationDto>(
                    """
                    SELECT
                        id,
                        internship_id,
                        student_user_id,
                        advisor_user_id,
                        evaluator_name,
                        evaluation_date,
                        status,
                        commitment_score,
                        communication_score,
                        technical_score,
                        behavior_score,
                        total_percentage,
                        strengths,
                        improvement_areas,
                        advisor_notes,
                        created_at,
                        updated_at
                    FROM academic_student_evaluations
                    WHERE student_user_id = @studentUserId
                    ORDER BY updated_at DESC, id DESC
                    """,
                    new NpgsqlParameter("studentUserId", userId.Value))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpPost("upsert")]
        [Authorize(Roles = "AcademicAdvisor,Administrator")]
        public async Task<IActionResult> Upsert([FromBody] UpsertAcademicStudentEvaluationRequest request)
        {
            if (request.internship_id <= 0)
                return BadRequest(new { message = "internship_id is required." });

            if (request.student_user_id <= 0)
                return BadRequest(new { message = "student_user_id is required." });

            if (request.advisor_user_id <= 0)
                return BadRequest(new { message = "advisor_user_id is required." });

            var totalPercentage =
                (request.commitment_score ?? 0) +
                (request.communication_score ?? 0) +
                (request.technical_score ?? 0) +
                (request.behavior_score ?? 0);

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO academic_student_evaluations
                    (
                        internship_id,
                        student_user_id,
                        advisor_user_id,
                        evaluator_name,
                        evaluation_date,
                        status,
                        commitment_score,
                        communication_score,
                        technical_score,
                        behavior_score,
                        total_percentage,
                        strengths,
                        improvement_areas,
                        advisor_notes,
                        created_at,
                        updated_at
                    )
                VALUES
                    (
                        {request.internship_id},
                        {request.student_user_id},
                        {request.advisor_user_id},
                        {request.evaluator_name},
                        {request.evaluation_date ?? DateOnly.FromDateTime(DateTime.Today)},
                        {request.status ?? "Draft"},
                        {request.commitment_score ?? 0},
                        {request.communication_score ?? 0},
                        {request.technical_score ?? 0},
                        {request.behavior_score ?? 0},
                        {totalPercentage},
                        {request.strengths},
                        {request.improvement_areas},
                        {request.advisor_notes},
                        NOW(),
                        NOW()
                    )
                ON CONFLICT (internship_id, student_user_id, advisor_user_id)
                DO UPDATE SET
                    evaluator_name = EXCLUDED.evaluator_name,
                    evaluation_date = EXCLUDED.evaluation_date,
                    status = EXCLUDED.status,
                    commitment_score = EXCLUDED.commitment_score,
                    communication_score = EXCLUDED.communication_score,
                    technical_score = EXCLUDED.technical_score,
                    behavior_score = EXCLUDED.behavior_score,
                    total_percentage = EXCLUDED.total_percentage,
                    strengths = EXCLUDED.strengths,
                    improvement_areas = EXCLUDED.improvement_areas,
                    advisor_notes = EXCLUDED.advisor_notes,
                    updated_at = NOW()
                """);

            return Ok(new
            {
                message = "Academic advisor evaluation saved successfully.",
                total_percentage = totalPercentage
            });
        }

        private long? GetCurrentUserId()
        {
            var raw =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub");

            return long.TryParse(raw, out var id) ? id : null;
        }
    }

    public class UpsertAcademicStudentEvaluationRequest
    {
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public long advisor_user_id { get; set; }
        public string? evaluator_name { get; set; }
        public DateOnly? evaluation_date { get; set; }
        public string? status { get; set; }

        public decimal? commitment_score { get; set; }
        public decimal? communication_score { get; set; }
        public decimal? technical_score { get; set; }
        public decimal? behavior_score { get; set; }

        public string? strengths { get; set; }
        public string? improvement_areas { get; set; }
        public string? advisor_notes { get; set; }
    }

    public class AcademicStudentEvaluationDto
    {
        public long id { get; set; }
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public long advisor_user_id { get; set; }
        public string? evaluator_name { get; set; }
        public DateOnly evaluation_date { get; set; }
        public string status { get; set; } = string.Empty;

        public decimal commitment_score { get; set; }
        public decimal communication_score { get; set; }
        public decimal technical_score { get; set; }
        public decimal behavior_score { get; set; }
        public decimal total_percentage { get; set; }

        public string? strengths { get; set; }
        public string? improvement_areas { get; set; }
        public string? advisor_notes { get; set; }

        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }
}
