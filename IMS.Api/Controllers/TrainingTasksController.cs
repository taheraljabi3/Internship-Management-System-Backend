using IMS.Api.Data;
using IMS.Api.DTOs;
using IMS.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainingTasksController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TrainingTasksController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("internship/{internshipId:long}")]
        public async Task<IActionResult> GetByInternship(long internshipId, [FromQuery] int? weekNo = null)
        {
            var sql = """
                SELECT
                    t.id,
                    t.training_plan_id,
                    t.internship_id,
                    t.student_user_id,
                    t.task_date,
                    t.week_no,
                    t.task_title,
                    t.created_at,
                    t.updated_at,
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
                  AND (@weekNo IS NULL OR t.week_no = @weekNo)
                ORDER BY t.task_date DESC, t.id DESC
                """;

            var internshipIdParam = new NpgsqlParameter("internshipId", NpgsqlDbType.Bigint)
            {
                Value = internshipId
            };

            var weekNoParam = new NpgsqlParameter("weekNo", NpgsqlDbType.Integer)
            {
                Value = (object?)weekNo ?? DBNull.Value
            };

            var tasks = await _context.Database
                .SqlQueryRaw<TrainingTaskWithEvidenceCountRow>(sql, internshipIdParam, weekNoParam)
                .ToListAsync();

            var taskIds = tasks.Select(x => x.id).ToArray();

            var evidences = new List<TrainingTaskEvidenceListItemDto>();

            if (taskIds.Length > 0)
            {
                var taskIdsParam = new NpgsqlParameter("taskIds", NpgsqlDbType.Array | NpgsqlDbType.Bigint)
                {
                    Value = taskIds
                };

                evidences = await _context.Database
                    .SqlQueryRaw<TrainingTaskEvidenceListItemDto>(
                        """
                        SELECT
                            id,
                            task_id,
                            file_name,
                            file_url,
                            uploaded_at
                        FROM training_task_evidences
                        WHERE task_id = ANY(@taskIds)
                        ORDER BY uploaded_at DESC, id DESC
                        """,
                        taskIdsParam)
                    .ToListAsync();
            }

            var result = tasks.Select(task => new
            {
                task.id,
                task.training_plan_id,
                task.internship_id,
                task.student_user_id,
                task.task_date,
                task.week_no,
                task.task_title,
                title = task.task_title,
                task.created_at,
                task.updated_at,
                task.evidence_count,
                evidences = evidences
                    .Where(evidence => evidence.task_id == task.id)
                    .Select(evidence => new
                    {
                        evidence.id,
                        evidence.task_id,
                        evidence.file_name,
                        evidence.file_url,
                        evidence.uploaded_at
                    })
                    .ToList()
            });

            return Ok(result);
        }

        [HttpGet("{taskId:long}/evidences")]
        public async Task<IActionResult> GetEvidences(long taskId)
        {
            var items = await _context.Database
                .SqlQueryRaw<TrainingTaskEvidenceListItemDto>(
                    """
                    SELECT
                        id,
                        task_id,
                        file_name,
                        file_url,
                        uploaded_at
                    FROM training_task_evidences
                    WHERE task_id = @taskId
                    ORDER BY uploaded_at DESC, id DESC
                    """,
                    new NpgsqlParameter("taskId", taskId))
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateTrainingTaskRequest request)
        {
            if (request.internship_id <= 0)
                return BadRequest(new { message = "internship_id is required." });

            if (request.training_plan_id <= 0)
                return BadRequest(new { message = "training_plan_id is required." });

            if (request.student_user_id <= 0)
                return BadRequest(new { message = "student_user_id is required." });

            if (string.IsNullOrWhiteSpace(request.task_title))
                return BadRequest(new { message = "task_title is required." });

            var planExists = await _context.Database.SqlQueryRaw<long>(
                    """
                    SELECT id AS "Value"
                    FROM training_plans
                    WHERE id = @planId
                      AND internship_id = @internshipId
                      AND student_user_id = @studentUserId
                      AND status = 'Approved'::approval_status_enum
                    LIMIT 1
                    """,
                    new NpgsqlParameter("planId", request.training_plan_id),
                    new NpgsqlParameter("internshipId", request.internship_id),
                    new NpgsqlParameter("studentUserId", request.student_user_id))
                .AnyAsync();

            if (!planExists)
                return BadRequest(new { message = "Approved training plan was not found for this student and internship." });

            var taskId = _context.Database.SqlQuery<long>($"""
                INSERT INTO training_tasks
                    (
                        internship_id,
                        training_plan_id,
                        student_user_id,
                        task_date,
                        week_no,
                        task_title,
                        created_at,
                        updated_at
                    )
                VALUES
                    (
                        {request.internship_id},
                        {request.training_plan_id},
                        {request.student_user_id},
                        {request.task_date},
                        {request.week_no},
                        {request.task_title},
                        NOW(),
                        NOW()
                    )
                RETURNING id AS "Value"
                """).AsEnumerable().Single();

            await WorkflowWriteHelper.QueueInAppAsync(
                _context,
                request.student_user_id,
                "Daily Task Saved",
                "Your daily task was saved successfully.",
                "training_tasks",
                taskId
            );

            return Ok(new
            {
                id = taskId,
                message = "Training task created successfully."
            });
        }

        [HttpPost("{taskId:long}/evidences")]
        public async Task<IActionResult> AddEvidence(long taskId, AddTrainingTaskEvidenceRequest request)
        {
            var taskExists = await _context.Database.SqlQueryRaw<long>(
                    """
                    SELECT id AS "Value"
                    FROM training_tasks
                    WHERE id = @taskId
                    LIMIT 1
                    """,
                    new NpgsqlParameter("taskId", taskId))
                .AnyAsync();

            if (!taskExists)
                return NotFound(new { message = "Training task not found." });

            if (string.IsNullOrWhiteSpace(request.file_name))
                return BadRequest(new { message = "file_name is required." });

            if (string.IsNullOrWhiteSpace(request.file_url))
                return BadRequest(new { message = "file_url is required." });

            var evidenceId = _context.Database.SqlQuery<long>($"""
                INSERT INTO training_task_evidences
                    (
                        task_id,
                        file_name,
                        file_url,
                        uploaded_at
                    )
                VALUES
                    (
                        {taskId},
                        {request.file_name},
                        {request.file_url},
                        NOW()
                    )
                RETURNING id AS "Value"
                """).AsEnumerable().Single();

            var evidence = await _context.Database
                .SqlQueryRaw<TrainingTaskEvidenceListItemDto>(
                    """
                    SELECT
                        id,
                        task_id,
                        file_name,
                        file_url,
                        uploaded_at
                    FROM training_task_evidences
                    WHERE id = @evidenceId
                    LIMIT 1
                    """,
                    new NpgsqlParameter("evidenceId", evidenceId))
                .FirstOrDefaultAsync();

            return Ok(new
            {
                id = evidenceId,
                evidence,
                message = "Evidence added successfully."
            });
        }

        [HttpDelete("{taskId:long}")]
        public async Task<IActionResult> Delete(long taskId)
        {
            var exists = await _context.training_tasks.AnyAsync(x => x.id == taskId);
            if (!exists)
                return NotFound(new { message = "Training task not found." });

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM training_tasks
                WHERE id = {taskId}
                """);

            return Ok(new { message = "Training task deleted successfully." });
        }
    }

    public class TrainingTaskWithEvidenceCountRow
    {
        public long id { get; set; }
        public long training_plan_id { get; set; }
        public long internship_id { get; set; }
        public long student_user_id { get; set; }
        public DateTime task_date { get; set; }
        public int week_no { get; set; }
        public string task_title { get; set; } = string.Empty;
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public int evidence_count { get; set; }
    }

    public class TrainingTaskEvidenceListItemDto
    {
        public long id { get; set; }
        public long task_id { get; set; }
        public string? file_name { get; set; }
        public string? file_url { get; set; }
        public DateTime uploaded_at { get; set; }
    }
}