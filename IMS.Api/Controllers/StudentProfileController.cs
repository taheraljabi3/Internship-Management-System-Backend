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
    public class StudentProfileController : ControllerBase
    {
        private readonly AppDbContext _context;

        private static readonly HashSet<string> AllowedDocumentCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            "CV", "Portfolio", "Certificate", "OtherAttachment", "TrainingLetter"
        };

        private static readonly HashSet<string> AllowedFileTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "PDF", "DOCX", "PPTX", "PNG", "JPG", "JPEG", "OTHER"
        };

        private static readonly HashSet<string> AllowedDocumentStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Uploaded", "Reviewed", "Archived", "Generated"
        };

        private static readonly HashSet<string> AllowedSkillLevels = new(StringComparer.OrdinalIgnoreCase)
        {
            "Beginner", "Intermediate", "Advanced"
        };

        public StudentProfileController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            var rows = await _context.Database
                .SqlQueryRaw<StudentProfileMeDto>(
                    """
                    SELECT
                        u.id AS user_id,
                        sp.student_code,
                        sp.headline,
                        sp.university,
                        sp.major,
                        sp.gpa,
                        sp.city,
                        sp.country,
                        sp.graduation_year,
                        sp.linked_in_url,
                        sp.photo_url,
                        sp.bio,
                        adv.full_name AS academic_advisor_name,
                        adv.email AS academic_advisor_email
                    FROM users u
                    LEFT JOIN student_profiles sp
                        ON sp.user_id = u.id
                    LEFT JOIN advisor_student_assignments asa
                        ON asa.student_user_id = u.id
                       AND asa.status = 'Active'::assignment_status_enum
                    LEFT JOIN users adv
                        ON adv.id = asa.advisor_user_id
                    WHERE u.id = @userId
                    LIMIT 1
                    """,
                    new NpgsqlParameter("userId", currentUserId.Value))
                .ToListAsync();

            var profile = rows.FirstOrDefault();
            if (profile == null)
                return NotFound(new { message = "Student profile was not found." });

            return Ok(profile);
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpsertMyProfile(UpdateStudentProfileRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO student_profiles
                    (
                        user_id,
                        student_code,
                        headline,
                        university,
                        major,
                        gpa,
                        city,
                        country,
                        graduation_year,
                        linked_in_url,
                        photo_url,
                        bio,
                        created_at,
                        updated_at
                    )
                VALUES
                    (
                        {currentUserId.Value},
                        {request.student_code},
                        {request.headline},
                        {request.university},
                        {request.major},
                        {request.gpa},
                        {request.city},
                        {request.country},
                        {request.graduation_year},
                        {request.linked_in_url},
                        {request.photo_url},
                        {request.bio},
                        NOW(),
                        NOW()
                    )
                ON CONFLICT (user_id)
                DO UPDATE SET
                    student_code = EXCLUDED.student_code,
                    headline = EXCLUDED.headline,
                    university = EXCLUDED.university,
                    major = EXCLUDED.major,
                    gpa = EXCLUDED.gpa,
                    city = EXCLUDED.city,
                    country = EXCLUDED.country,
                    graduation_year = EXCLUDED.graduation_year,
                    linked_in_url = EXCLUDED.linked_in_url,
                    photo_url = EXCLUDED.photo_url,
                    bio = EXCLUDED.bio,
                    updated_at = NOW()
                """);

            return Ok(new { message = "Student profile saved successfully." });
        }

        [HttpGet("me/documents")]
        public async Task<IActionResult> GetMyDocuments()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

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
                    new NpgsqlParameter("studentUserId", currentUserId.Value))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpPost("me/documents")]
        public async Task<IActionResult> CreateMyDocument(CreateStudentDocumentRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            if (string.IsNullOrWhiteSpace(request.title))
                return BadRequest(new { message = "title is required." });

            if (string.IsNullOrWhiteSpace(request.file_name))
                return BadRequest(new { message = "file_name is required." });

            if (!AllowedDocumentCategories.Contains(request.category ?? string.Empty))
                return BadRequest(new { message = "Invalid document category." });

            if (!AllowedFileTypes.Contains(request.file_type ?? string.Empty))
                return BadRequest(new { message = "Invalid file type." });

            if (!AllowedDocumentStatuses.Contains(request.status ?? string.Empty))
                return BadRequest(new { message = "Invalid document status." });

            var documentId = _context.Database.SqlQuery<long>($"""
                INSERT INTO student_documents
                    (
                        student_user_id,
                        title,
                        file_name,
                        file_url,
                        category,
                        file_type,
                        status,
                        uploaded_at,
                        description,
                        generated_by_user_id
                    )
                VALUES
                    (
                        {currentUserId.Value},
                        {request.title},
                        {request.file_name},
                        {request.file_url},
                        {request.category}::document_category_enum,
                        {request.file_type}::file_type_enum,
                        {request.status}::document_status_enum,
                        NOW(),
                        {request.description},
                        {currentUserId.Value}
                    )
                RETURNING id AS "Value"
                """).AsEnumerable().Single();

            return Ok(new
            {
                id = documentId,
                message = "Document created successfully."
            });
        }

        [HttpPut("me/documents/{id:long}")]
        public async Task<IActionResult> UpdateMyDocument(long id, UpdateStudentDocumentRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            if (string.IsNullOrWhiteSpace(request.title))
                return BadRequest(new { message = "title is required." });

            if (string.IsNullOrWhiteSpace(request.file_name))
                return BadRequest(new { message = "file_name is required." });

            if (!AllowedDocumentCategories.Contains(request.category ?? string.Empty))
                return BadRequest(new { message = "Invalid document category." });

            if (!AllowedFileTypes.Contains(request.file_type ?? string.Empty))
                return BadRequest(new { message = "Invalid file type." });

            if (!AllowedDocumentStatuses.Contains(request.status ?? string.Empty))
                return BadRequest(new { message = "Invalid document status." });

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE student_documents
                SET
                    title = {request.title},
                    file_name = {request.file_name},
                    file_url = {request.file_url},
                    category = {request.category}::document_category_enum,
                    file_type = {request.file_type}::file_type_enum,
                    status = {request.status}::document_status_enum,
                    description = {request.description}
                WHERE id = {id}
                  AND student_user_id = {currentUserId.Value}
                """);

            if (affected == 0)
                return NotFound(new { message = "Document not found." });

            return Ok(new { message = "Document updated successfully." });
        }

        [HttpDelete("me/documents/{id:long}")]
        public async Task<IActionResult> DeleteMyDocument(long id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM student_documents
                WHERE id = {id}
                  AND student_user_id = {currentUserId.Value}
                """);

            if (affected == 0)
                return NotFound(new { message = "Document not found." });

            return Ok(new { message = "Document deleted successfully." });
        }

        [HttpGet("me/skills")]
        public async Task<IActionResult> GetMySkills()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

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
                    new NpgsqlParameter("studentUserId", currentUserId.Value))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpPost("me/skills")]
        public async Task<IActionResult> CreateMySkill(CreateStudentSkillRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            if (string.IsNullOrWhiteSpace(request.name))
                return BadRequest(new { message = "name is required." });

            if (!AllowedSkillLevels.Contains(request.level ?? string.Empty))
                return BadRequest(new { message = "Invalid skill level." });

            var skillId = _context.Database.SqlQuery<long>($"""
                INSERT INTO student_skills
                    (
                        student_user_id,
                        name,
                        level,
                        category,
                        created_at
                    )
                VALUES
                    (
                        {currentUserId.Value},
                        {request.name},
                        {request.level}::skill_level_enum,
                        {request.category},
                        NOW()
                    )
                RETURNING id AS "Value"
                """).AsEnumerable().Single();

            return Ok(new
            {
                id = skillId,
                message = "Skill created successfully."
            });
        }

        [HttpPut("me/skills/{id:long}")]
        public async Task<IActionResult> UpdateMySkill(long id, UpdateStudentSkillRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            if (string.IsNullOrWhiteSpace(request.name))
                return BadRequest(new { message = "name is required." });

            if (!AllowedSkillLevels.Contains(request.level ?? string.Empty))
                return BadRequest(new { message = "Invalid skill level." });

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE student_skills
                SET
                    name = {request.name},
                    level = {request.level}::skill_level_enum,
                    category = {request.category}
                WHERE id = {id}
                  AND student_user_id = {currentUserId.Value}
                """);

            if (affected == 0)
                return NotFound(new { message = "Skill not found." });

            return Ok(new { message = "Skill updated successfully." });
        }

        [HttpDelete("me/skills/{id:long}")]
        public async Task<IActionResult> DeleteMySkill(long id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM student_skills
                WHERE id = {id}
                  AND student_user_id = {currentUserId.Value}
                """);

            if (affected == 0)
                return NotFound(new { message = "Skill not found." });

            return Ok(new { message = "Skill deleted successfully." });
        }

        [HttpGet("me/projects")]
        public async Task<IActionResult> GetMyProjects()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

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
                    new NpgsqlParameter("studentUserId", currentUserId.Value))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpPost("me/projects")]
        public async Task<IActionResult> CreateMyProject(CreateStudentProjectRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            if (string.IsNullOrWhiteSpace(request.title))
                return BadRequest(new { message = "title is required." });

            var projectId = _context.Database.SqlQuery<long>($"""
                INSERT INTO student_projects
                    (
                        student_user_id,
                        title,
                        project_year,
                        role_name,
                        project_link,
                        description,
                        created_at
                    )
                VALUES
                    (
                        {currentUserId.Value},
                        {request.title},
                        {request.project_year},
                        {request.role_name},
                        {request.project_link},
                        {request.description},
                        NOW()
                    )
                RETURNING id AS "Value"
                """).AsEnumerable().Single();

            return Ok(new
            {
                id = projectId,
                message = "Project created successfully."
            });
        }

        [HttpPut("me/projects/{id:long}")]
        public async Task<IActionResult> UpdateMyProject(long id, UpdateStudentProjectRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            if (string.IsNullOrWhiteSpace(request.title))
                return BadRequest(new { message = "title is required." });

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE student_projects
                SET
                    title = {request.title},
                    project_year = {request.project_year},
                    role_name = {request.role_name},
                    project_link = {request.project_link},
                    description = {request.description}
                WHERE id = {id}
                  AND student_user_id = {currentUserId.Value}
                """);

            if (affected == 0)
                return NotFound(new { message = "Project not found." });

            return Ok(new { message = "Project updated successfully." });
        }

        [HttpDelete("me/projects/{id:long}")]
        public async Task<IActionResult> DeleteMyProject(long id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM student_projects
                WHERE id = {id}
                  AND student_user_id = {currentUserId.Value}
                """);

            if (affected == 0)
                return NotFound(new { message = "Project not found." });

            return Ok(new { message = "Project deleted successfully." });
        }

        [HttpGet("me/courses")]
        public async Task<IActionResult> GetMyCourses()
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

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
                    new NpgsqlParameter("studentUserId", currentUserId.Value))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpPost("me/courses")]
        public async Task<IActionResult> CreateMyCourse(CreateStudentCourseRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            if (string.IsNullOrWhiteSpace(request.title))
                return BadRequest(new { message = "title is required." });

            var courseId = _context.Database.SqlQuery<long>($"""
                INSERT INTO student_courses
                    (
                        student_user_id,
                        title,
                        provider,
                        hours,
                        course_year,
                        created_at
                    )
                VALUES
                    (
                        {currentUserId.Value},
                        {request.title},
                        {request.provider},
                        {request.hours},
                        {request.course_year},
                        NOW()
                    )
                RETURNING id AS "Value"
                """).AsEnumerable().Single();

            return Ok(new
            {
                id = courseId,
                message = "Course created successfully."
            });
        }

        [HttpPut("me/courses/{id:long}")]
        public async Task<IActionResult> UpdateMyCourse(long id, UpdateStudentCourseRequest request)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            if (string.IsNullOrWhiteSpace(request.title))
                return BadRequest(new { message = "title is required." });

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE student_courses
                SET
                    title = {request.title},
                    provider = {request.provider},
                    hours = {request.hours},
                    course_year = {request.course_year}
                WHERE id = {id}
                  AND student_user_id = {currentUserId.Value}
                """);

            if (affected == 0)
                return NotFound(new { message = "Course not found." });

            return Ok(new { message = "Course updated successfully." });
        }

[HttpPost("me/documents/upload")]
[RequestSizeLimit(10_000_000)]
public async Task<IActionResult> UploadMyDocument([FromForm] UploadStudentDocumentRequest request)
{
    var currentUserId = GetCurrentUserId();
    if (!currentUserId.HasValue)
        return Unauthorized(new { message = "Invalid user session." });

    if (request.File == null || request.File.Length == 0)
        return BadRequest(new { message = "file is required." });

    var category = string.IsNullOrWhiteSpace(request.Category) ? "OtherAttachment" : request.Category.Trim();
    var status = string.IsNullOrWhiteSpace(request.Status) ? "Uploaded" : request.Status.Trim();
    var originalFileName = Path.GetFileName(request.File.FileName);
    var fileType = ResolveFileType(originalFileName);
    var title = string.IsNullOrWhiteSpace(request.Title)
        ? Path.GetFileNameWithoutExtension(originalFileName)
        : request.Title.Trim();

    if (string.IsNullOrWhiteSpace(title))
        return BadRequest(new { message = "title is required." });

    if (!AllowedDocumentCategories.Contains(category))
        return BadRequest(new { message = "Invalid document category." });

    if (!AllowedDocumentStatuses.Contains(status))
        return BadRequest(new { message = "Invalid document status." });

    if (!AllowedFileTypes.Contains(fileType))
        return BadRequest(new { message = "Invalid file type." });

    var fileUrl = await SaveStudentDocumentFileAsync(currentUserId.Value, request.File);

    var documentId = _context.Database.SqlQuery<long>($"""
        INSERT INTO student_documents
            (
                student_user_id,
                title,
                file_name,
                file_url,
                category,
                file_type,
                status,
                uploaded_at,
                description,
                generated_by_user_id
            )
        VALUES
            (
                {currentUserId.Value},
                {title},
                {originalFileName},
                {fileUrl},
                {category}::document_category_enum,
                {fileType}::file_type_enum,
                {status}::document_status_enum,
                NOW(),
                {request.Description},
                {currentUserId.Value}
            )
        RETURNING id AS "Value"
        """).AsEnumerable().Single();

    return Ok(new
    {
        id = documentId,
        title,
        file_name = originalFileName,
        file_url = fileUrl,
        category,
        file_type = fileType,
        status,
        message = "Document uploaded successfully."
    });
}

[HttpPut("me/documents/{id:long}/file")]
[RequestSizeLimit(10_000_000)]
public async Task<IActionResult> ReplaceMyDocumentFile(long id, [FromForm] UploadStudentDocumentRequest request)
{
    var currentUserId = GetCurrentUserId();
    if (!currentUserId.HasValue)
        return Unauthorized(new { message = "Invalid user session." });

    if (request.File == null || request.File.Length == 0)
        return BadRequest(new { message = "file is required." });

    var existingId = await _context.Database.SqlQueryRaw<long>(
        """
        SELECT id AS "Value"
        FROM student_documents
        WHERE id = @id
          AND student_user_id = @studentUserId
        LIMIT 1
        """,
        new Npgsql.NpgsqlParameter("id", id),
        new Npgsql.NpgsqlParameter("studentUserId", currentUserId.Value))
        .FirstOrDefaultAsync();

    if (existingId <= 0)
        return NotFound(new { message = "Document not found." });

    var category = string.IsNullOrWhiteSpace(request.Category) ? "OtherAttachment" : request.Category.Trim();
    var status = string.IsNullOrWhiteSpace(request.Status) ? "Uploaded" : request.Status.Trim();
    var originalFileName = Path.GetFileName(request.File.FileName);
    var fileType = ResolveFileType(originalFileName);
    var title = string.IsNullOrWhiteSpace(request.Title)
        ? Path.GetFileNameWithoutExtension(originalFileName)
        : request.Title.Trim();

    if (string.IsNullOrWhiteSpace(title))
        return BadRequest(new { message = "title is required." });

    if (!AllowedDocumentCategories.Contains(category))
        return BadRequest(new { message = "Invalid document category." });

    if (!AllowedDocumentStatuses.Contains(status))
        return BadRequest(new { message = "Invalid document status." });

    if (!AllowedFileTypes.Contains(fileType))
        return BadRequest(new { message = "Invalid file type." });

    var fileUrl = await SaveStudentDocumentFileAsync(currentUserId.Value, request.File);

    await _context.Database.ExecuteSqlInterpolatedAsync($"""
        UPDATE student_documents
        SET
            title = {title},
            file_name = {originalFileName},
            file_url = {fileUrl},
            category = {category}::document_category_enum,
            file_type = {fileType}::file_type_enum,
            status = {status}::document_status_enum,
            description = {request.Description}
        WHERE id = {id}
          AND student_user_id = {currentUserId.Value}
        """);

    return Ok(new
    {
        id,
        title,
        file_name = originalFileName,
        file_url = fileUrl,
        category,
        file_type = fileType,
        status,
        message = "Document file replaced successfully."
    });
}

private static string ResolveFileType(string fileName)
{
    var extension = Path.GetExtension(fileName).TrimStart('.').ToUpperInvariant();

    return extension switch
    {
        "PDF" => "PDF",
        "DOCX" => "DOCX",
        "PPTX" => "PPTX",
        "PNG" => "PNG",
        "JPG" => "JPG",
        "JPEG" => "JPEG",
        _ => "OTHER"
    };
}

private static async Task<string> SaveStudentDocumentFileAsync(long studentUserId, IFormFile file)
{
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    var storedFileName = $"{Guid.NewGuid():N}{extension}";

    var uploadsDirectory = Path.Combine(
        Directory.GetCurrentDirectory(),
        "wwwroot",
        "uploads",
        "student-documents",
        studentUserId.ToString());

    Directory.CreateDirectory(uploadsDirectory);

    var fullPath = Path.Combine(uploadsDirectory, storedFileName);

    await using var stream = System.IO.File.Create(fullPath);
    await file.CopyToAsync(stream);

    return $"/uploads/student-documents/{studentUserId}/{storedFileName}";
}

public class UploadStudentDocumentRequest
{
    public IFormFile? File { get; set; }
    public string? Title { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? Description { get; set; }
}
        [HttpDelete("me/courses/{id:long}")]
        public async Task<IActionResult> DeleteMyCourse(long id)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized(new { message = "Invalid user session." });

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM student_courses
                WHERE id = {id}
                  AND student_user_id = {currentUserId.Value}
                """);

            if (affected == 0)
                return NotFound(new { message = "Course not found." });

            return Ok(new { message = "Course deleted successfully." });
        }

        private long? GetCurrentUserId()
        {
            var raw =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub");

            return long.TryParse(raw, out var id) ? id : null;
        }
    }

    public class UpdateStudentProfileRequest
    {
        public string? student_code { get; set; }
        public string? headline { get; set; }
        public string? university { get; set; }
        public string? major { get; set; }
        public decimal? gpa { get; set; }
        public string? city { get; set; }
        public string? country { get; set; }
        public int? graduation_year { get; set; }
        public string? linked_in_url { get; set; }
        public string? photo_url { get; set; }
        public string? bio { get; set; }
    }

    public class CreateStudentDocumentRequest
    {
        public string? title { get; set; }
        public string? file_name { get; set; }
        public string? file_url { get; set; }
        public string? category { get; set; }
        public string? file_type { get; set; }
        public string? status { get; set; }
        public string? description { get; set; }
    }

    public class UpdateStudentDocumentRequest : CreateStudentDocumentRequest
    {
    }

    public class CreateStudentSkillRequest
    {
        public string? name { get; set; }
        public string? level { get; set; }
        public string? category { get; set; }
    }

    public class UpdateStudentSkillRequest : CreateStudentSkillRequest
    {
    }

    public class CreateStudentProjectRequest
    {
        public string? title { get; set; }
        public int? project_year { get; set; }
        public string? role_name { get; set; }
        public string? project_link { get; set; }
        public string? description { get; set; }
    }

    public class UpdateStudentProjectRequest : CreateStudentProjectRequest
    {
    }

    public class CreateStudentCourseRequest
    {
        public string? title { get; set; }
        public string? provider { get; set; }
        public int? hours { get; set; }
        public int? course_year { get; set; }
    }

    public class UpdateStudentCourseRequest : CreateStudentCourseRequest
    {
    }

    public class StudentProfileMeDto
    {
        public long user_id { get; set; }
        public string? student_code { get; set; }
        public string? headline { get; set; }
        public string? university { get; set; }
        public string? major { get; set; }
        public decimal? gpa { get; set; }
        public string? city { get; set; }
        public string? country { get; set; }
        public int? graduation_year { get; set; }
        public string? linked_in_url { get; set; }
        public string? photo_url { get; set; }
        public string? bio { get; set; }
        public string? academic_advisor_name { get; set; }
        public string? academic_advisor_email { get; set; }
    }

    public class StudentDocumentDto
    {
        public long id { get; set; }
        public long student_user_id { get; set; }
        public string? title { get; set; }
        public string? file_name { get; set; }
        public string? file_url { get; set; }
        public string? category { get; set; }
        public string? file_type { get; set; }
        public string? status { get; set; }
        public DateTime uploaded_at { get; set; }
        public string? description { get; set; }
    }

    public class StudentSkillDto
    {
        public long id { get; set; }
        public long student_user_id { get; set; }
        public string? name { get; set; }
        public string? level { get; set; }
        public string? category { get; set; }
        public DateTime created_at { get; set; }
    }

    public class StudentProjectDto
    {
        public long id { get; set; }
        public long student_user_id { get; set; }
        public string? title { get; set; }
        public int? project_year { get; set; }
        public string? role_name { get; set; }
        public string? project_link { get; set; }
        public string? description { get; set; }
        public DateTime created_at { get; set; }
    }

    public class StudentCourseDto
    {
        public long id { get; set; }
        public long student_user_id { get; set; }
        public string? title { get; set; }
        public string? provider { get; set; }
        public int? hours { get; set; }
        public int? course_year { get; set; }
        public DateTime created_at { get; set; }
    }
}