using IMS.Api.Data;
using IMS.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StudentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var students = await (
                from sp in _context.student_profiles.AsNoTracking()
                join u in _context.users.AsNoTracking()
                    on sp.user_id equals u.id
                join adv in _context.vw_student_current_advisors.AsNoTracking()
                    on sp.user_id equals adv.student_user_id into advisorJoin
                from advisor in advisorJoin.DefaultIfEmpty()
                select new StudentListItemDto
                {
                    user_id = sp.user_id,
                    full_name = u.full_name,
                    email = u.email,
                    student_code = sp.student_code,
                    university = sp.university,
                    major = sp.major,
                    gpa = sp.gpa,
                    advisor_user_id = advisor != null ? advisor.advisor_user_id : null,
                    advisor_name = advisor != null ? advisor.advisor_name : null,
                    advisor_email = advisor != null ? advisor.advisor_email : null
                }
            )
            .OrderBy(x => x.full_name)
            .ToListAsync();

            return Ok(students);
        }

        [HttpGet("{userId:long}")]
        public async Task<IActionResult> GetById(long userId)
        {
            var student = await (
                from sp in _context.student_profiles.AsNoTracking()
                join u in _context.users.AsNoTracking()
                    on sp.user_id equals u.id
                join adv in _context.vw_student_current_advisors.AsNoTracking()
                    on sp.user_id equals adv.student_user_id into advisorJoin
                from advisor in advisorJoin.DefaultIfEmpty()
                where sp.user_id == userId
                select new StudentListItemDto
                {
                    user_id = sp.user_id,
                    full_name = u.full_name,
                    email = u.email,
                    student_code = sp.student_code,
                    university = sp.university,
                    major = sp.major,
                    gpa = sp.gpa,
                    advisor_user_id = advisor != null ? advisor.advisor_user_id : null,
                    advisor_name = advisor != null ? advisor.advisor_name : null,
                    advisor_email = advisor != null ? advisor.advisor_email : null
                }
            )
            .FirstOrDefaultAsync();

            if (student == null)
                return NotFound(new { message = "Student not found" });

            return Ok(student);
        }

        [HttpGet("{userId:long}/documents")]
        public async Task<IActionResult> GetStudentDocuments(long userId)
        {
            var exists = await _context.student_profiles
                .AsNoTracking()
                .AnyAsync(x => x.user_id == userId);

            if (!exists)
                return NotFound(new { message = "Student not found" });

            var rows = await _context.Database
                .SqlQueryRaw<StudentDocumentDto>(
                    """
                    SELECT
                        id,
                        student_user_id,
                        title,
                        file_name,
                        file_url,
                        category::text AS category,
                        file_type::text AS file_type,
                        status::text AS status,
                        uploaded_at,
                        description
                    FROM student_documents
                    WHERE student_user_id = @studentUserId
                    ORDER BY uploaded_at DESC, id DESC
                    """,
                    new NpgsqlParameter("studentUserId", userId))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpGet("{userId:long}/skills")]
        public async Task<IActionResult> GetStudentSkills(long userId)
        {
            var exists = await _context.student_profiles
                .AsNoTracking()
                .AnyAsync(x => x.user_id == userId);

            if (!exists)
                return NotFound(new { message = "Student not found" });

            var rows = await _context.Database
                .SqlQueryRaw<StudentSkillDto>(
                    """
                    SELECT
                        id,
                        student_user_id,
                        name,
                        level::text AS level,
                        category,
                        created_at
                    FROM student_skills
                    WHERE student_user_id = @studentUserId
                    ORDER BY id DESC
                    """,
                    new NpgsqlParameter("studentUserId", userId))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpGet("{userId:long}/projects")]
        public async Task<IActionResult> GetStudentProjects(long userId)
        {
            var exists = await _context.student_profiles
                .AsNoTracking()
                .AnyAsync(x => x.user_id == userId);

            if (!exists)
                return NotFound(new { message = "Student not found" });

            var rows = await _context.Database
                .SqlQueryRaw<StudentProjectDto>(
                    """
                    SELECT
                        id,
                        student_user_id,
                        title,
                        project_year,
                        role_name,
                        project_link,
                        description,
                        created_at
                    FROM student_projects
                    WHERE student_user_id = @studentUserId
                    ORDER BY id DESC
                    """,
                    new NpgsqlParameter("studentUserId", userId))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpGet("{userId:long}/courses")]
        public async Task<IActionResult> GetStudentCourses(long userId)
        {
            var exists = await _context.student_profiles
                .AsNoTracking()
                .AnyAsync(x => x.user_id == userId);

            if (!exists)
                return NotFound(new { message = "Student not found" });

            var rows = await _context.Database
                .SqlQueryRaw<StudentCourseDto>(
                    """
                    SELECT
                        id,
                        student_user_id,
                        title,
                        provider,
                        hours,
                        course_year,
                        created_at
                    FROM student_courses
                    WHERE student_user_id = @studentUserId
                    ORDER BY id DESC
                    """,
                    new NpgsqlParameter("studentUserId", userId))
                .ToListAsync();

            return Ok(rows);
        }
    }
}