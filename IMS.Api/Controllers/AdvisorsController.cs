using IMS.Api.Data;
using IMS.Api.DTOs;
using IMS.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdvisorsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdvisorsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await (
                from ap in _context.academic_advisor_profiles.AsNoTracking()
                join u in _context.users.AsNoTracking() on ap.user_id equals u.id
                join v in _context.vw_student_current_advisors.AsNoTracking() on u.id equals v.advisor_user_id into studentJoin
                select new AdvisorListItemDto
                {
                    user_id = u.id,
                    full_name = u.full_name,
                    email = u.email,
                    employee_no = ap.employee_no,
                    department = ap.department,
                    is_system_responsible = ap.is_system_responsible,
                    students_count = studentJoin.Count()
                }
            )
            .OrderBy(x => x.full_name)
            .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{advisorUserId:long}/students")]
        public async Task<IActionResult> GetStudents(long advisorUserId)
        {
            var items = await (
                from v in _context.vw_student_current_advisors.AsNoTracking()
                join sp in _context.student_profiles.AsNoTracking() on v.student_user_id equals sp.user_id
                join u in _context.users.AsNoTracking() on sp.user_id equals u.id
                where v.advisor_user_id == advisorUserId
                select new AdvisorStudentItemDto
                {
                    student_user_id = sp.user_id,
                    full_name = u.full_name,
                    email = u.email,
                    student_code = sp.student_code,
                    university = sp.university,
                    major = sp.major,
                    gpa = sp.gpa,
                    assignment_start_at = v.assignment_start_at,
                    notes = v.notes
                }
            )
            .OrderBy(x => x.full_name)
            .ToListAsync();

            return Ok(items);
        }

        [HttpPost("assignments")]
        public async Task<IActionResult> AssignStudent(AssignStudentAdvisorRequest request)
        {
            var studentExists = await _context.student_profiles.AnyAsync(x => x.user_id == request.student_user_id);
            if (!studentExists)
                return BadRequest(new { message = "Student profile not found." });

            var advisorExists = await _context.academic_advisor_profiles.AnyAsync(x => x.user_id == request.advisor_user_id);
            if (!advisorExists)
                return BadRequest(new { message = "Academic advisor not found." });

            await using var tx = await _context.Database.BeginTransactionAsync();

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE advisor_student_assignments
                SET status = {"Inactive"}::assignment_status_enum,
                    assignment_end_at = NOW(),
                    updated_at = NOW()
                WHERE student_user_id = {request.student_user_id}
                AND status = {"Active"}::assignment_status_enum;
                """);

            var assignmentId = _context.Database.SqlQuery<long>($"""
                INSERT INTO advisor_student_assignments
                    (student_user_id, advisor_user_id, assigned_by_user_id, status, assignment_start_at, notes, created_at, updated_at)
                VALUES
                    ({request.student_user_id}, {request.advisor_user_id}, {request.assigned_by_user_id}, {"Active"}::assignment_status_enum, NOW(), {request.notes}, NOW(), NOW())
                RETURNING id AS "Value";
                """).AsEnumerable().Single();

            await WorkflowWriteHelper.QueueInAppAsync(
                _context,
                request.student_user_id,
                "Academic Advisor Assigned",
                "Your academic advisor assignment has been updated.",
                "advisor_student_assignments",
                assignmentId);

            await WorkflowWriteHelper.LogAuditAsync(
                _context,
                request.assigned_by_user_id,
                "Assign Student To Advisor",
                "advisor_student_assignments",
                assignmentId.ToString());

            await tx.CommitAsync();

            return Ok(new { id = assignmentId, message = "Student assigned successfully." });
        }
    }
}