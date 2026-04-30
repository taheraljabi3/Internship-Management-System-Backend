using IMS.Api.Data;
using IMS.Api.DTOs;
using IMS.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IMS.Api.Services;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PasswordService _passwordService;

        public UsersController(AppDbContext context, PasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? q = null,
            [FromQuery] string? role = null,
            [FromQuery] string? status = null)
        {
            var sql = """
                SELECT
                    u.id,
                    u.full_name,
                    u.email,
                    u.username,
                    u.phone,
                    r.code AS role,
                    u.status::text AS status
                FROM users u
                LEFT JOIN user_roles ur ON ur.user_id = u.id
                LEFT JOIN roles r ON r.id = ur.role_id
                WHERE
                    ((@q)::text IS NULL OR
                     LOWER(u.full_name) LIKE LOWER('%' || (@q)::text || '%') OR
                     LOWER(u.email) LIKE LOWER('%' || (@q)::text || '%') OR
                     LOWER(COALESCE(u.username, '')) LIKE LOWER('%' || (@q)::text || '%') OR
                     LOWER(COALESCE(r.code, '')) LIKE LOWER('%' || (@q)::text || '%'))
                AND ((@role)::text IS NULL OR r.code = (@role)::text)
                AND ((@status)::text IS NULL OR u.status::text = (@status)::text)
                ORDER BY u.full_name;
                """;

            var qParam = new Npgsql.NpgsqlParameter("q", NpgsqlTypes.NpgsqlDbType.Text)
            {
                Value = string.IsNullOrWhiteSpace(q) ? DBNull.Value : q.Trim()
            };

            var roleParam = new Npgsql.NpgsqlParameter("role", NpgsqlTypes.NpgsqlDbType.Text)
            {
                Value = string.IsNullOrWhiteSpace(role) || role == "All" ? DBNull.Value : role.Trim()
            };

            var statusParam = new Npgsql.NpgsqlParameter("status", NpgsqlTypes.NpgsqlDbType.Text)
            {
                Value = string.IsNullOrWhiteSpace(status) || status == "All" ? DBNull.Value : status.Trim()
            };

            var items = await _context.Database
                .SqlQueryRaw<UserListItemDto>(sql, qParam, roleParam, statusParam)
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.full_name))
                return BadRequest(new { message = "Full name is required." });

            if (string.IsNullOrWhiteSpace(request.email))
                return BadRequest(new { message = "Email is required." });

            if (string.IsNullOrWhiteSpace(request.password))
                return BadRequest(new { message = "Password is required." });

            if (string.IsNullOrWhiteSpace(request.role_code))
                return BadRequest(new { message = "Role is required." });

            var roleId = await _context.roles
                .AsNoTracking()
                .Where(x => x.code == request.role_code)
                .Select(x => x.id)
                .FirstOrDefaultAsync();

            if (roleId == 0)
                return BadRequest(new { message = "Invalid role_code." });

            if (request.role_code == "Student")
            {
                if (string.IsNullOrWhiteSpace(request.student_code))
                    return BadRequest(new { message = "Student code is required for student creation." });

                if (!request.advisor_user_id.HasValue || request.advisor_user_id.Value <= 0)
                    return BadRequest(new { message = "Academic advisor is required for student creation." });

                var advisorExists = await _context.academic_advisor_profiles
                    .AsNoTracking()
                    .AnyAsync(x => x.user_id == request.advisor_user_id.Value);

                if (!advisorExists)
                    return BadRequest(new { message = "Selected academic advisor was not found." });
            }

            var normalizedEmail = request.email.Trim().ToLower();

            var emailExists = await _context.users
                .AsNoTracking()
                .AnyAsync(x => x.email.ToLower() == normalizedEmail);

            if (emailExists)
                return BadRequest(new { message = "Email already exists." });

            if (!string.IsNullOrWhiteSpace(request.username))
            {
                var normalizedUsername = request.username.Trim().ToLower();

                var usernameExists = await _context.users
                    .AsNoTracking()
                    .AnyAsync(x => x.username != null && x.username.ToLower() == normalizedUsername);

                if (usernameExists)
                    return BadRequest(new { message = "Username already exists." });
            }

            var passwordHash = _passwordService.Hash(request.password);
            var userStatus = string.IsNullOrWhiteSpace(request.status) ? "Active" : request.status;

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                var userId = _context.Database.SqlQuery<long>($"""
                    INSERT INTO users
                        (
                            full_name,
                            email,
                            username,
                            phone,
                            password_hash,
                            status,
                            is_email_confirmed,
                            created_at,
                            updated_at,
                            created_by_user_id
                        )
                    VALUES
                        (
                            {request.full_name},
                            {request.email},
                            {request.username},
                            {request.phone},
                            {passwordHash},
                            {userStatus}::user_status_enum,
                            FALSE,
                            NOW(),
                            NOW(),
                            {request.created_by_user_id}
                        )
                    RETURNING id AS "Value";
                    """).AsEnumerable().Single();

                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO user_roles
                        (user_id, role_id, created_at)
                    VALUES
                        ({userId}, {roleId}, NOW());
                    """);

                if (request.role_code == "Student")
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($"""
                        INSERT INTO student_profiles
                            (
                                user_id,
                                student_code,
                                university,
                                major,
                                created_at,
                                updated_at
                            )
                        VALUES
                            (
                                {userId},
                                {request.student_code},
                                {request.university},
                                {request.major},
                                NOW(),
                                NOW()
                            );
                        """);

                    long? advisorAssignmentId = null;

                    if (request.advisor_user_id.HasValue && request.advisor_user_id.Value > 0)
                    {
                        advisorAssignmentId = _context.Database.SqlQuery<long>($"""
                            INSERT INTO advisor_student_assignments
                                (
                                    student_user_id,
                                    advisor_user_id,
                                    assigned_by_user_id,
                                    status,
                                    assignment_start_at,
                                    notes,
                                    created_at,
                                    updated_at
                                )
                            VALUES
                                (
                                    {userId},
                                    {request.advisor_user_id.Value},
                                    {request.created_by_user_id},
                                    {"Active"}::assignment_status_enum,
                                    NOW(),
                                    {"Assigned automatically during manual student creation"},
                                    NOW(),
                                    NOW()
                                )
                            RETURNING id AS "Value";
                            """).AsEnumerable().Single();
                    }

                    await _context.Database.ExecuteSqlInterpolatedAsync($"""
                        INSERT INTO student_eligibility_reviews
                            (
                                student_user_id,
                                invitation_recipient_id,
                                advisor_assignment_id,
                                approval_owner_user_id,
                                approval_owner_role,
                                reviewer_user_id,
                                status,
                                comment,
                                reviewed_at,
                                approved_at,
                                rejected_at,
                                created_at,
                                updated_at
                            )
                        VALUES
                            (
                                {userId},
                                {null},
                                {advisorAssignmentId},
                                {request.advisor_user_id},
                                {"AcademicAdvisor"}::approver_type_enum,
                                {request.created_by_user_id},
                                {"Approved"}::approval_status_enum,
                                {"Approved automatically during manual student creation"},
                                NOW(),
                                NOW(),
                                {null},
                                NOW(),
                                NOW()
                            );
                        """);
                }
                else if (request.role_code == "AcademicAdvisor")
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($"""
                        INSERT INTO academic_advisor_profiles
                            (
                                user_id,
                                employee_no,
                                department,
                                is_system_responsible,
                                created_at,
                                updated_at
                            )
                        VALUES
                            (
                                {userId},
                                {request.employee_no},
                                {request.department},
                                {request.is_system_responsible},
                                NOW(),
                                NOW()
                            );
                        """);
                }
                else if (request.role_code == "Administrator")
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($"""
                        INSERT INTO administrator_profiles
                            (
                                user_id,
                                employee_no,
                                department,
                                created_at,
                                updated_at
                            )
                        VALUES
                            (
                                {userId},
                                {request.employee_no},
                                {request.department},
                                NOW(),
                                NOW()
                            );
                        """);
                }

                await WorkflowWriteHelper.LogAuditAsync(
                    _context,
                    request.created_by_user_id,
                    "Create User",
                    "users",
                    userId.ToString());

                await tx.CommitAsync();

                return Ok(new
                {
                    id = userId,
                    message = "User created successfully."
                });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, UpdateUserRequest request)
        {
            var exists = await _context.users.AnyAsync(x => x.id == id);
            if (!exists)
                return NotFound(new { message = "User not found." });

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE users
                SET full_name = {request.full_name},
                    email = {request.email},
                    username = {request.username},
                    phone = {request.phone},
                    status = {request.status}::user_status_enum,
                    updated_at = NOW()
                WHERE id = {id};
                """);

            return Ok(new { message = "User updated successfully." });
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var exists = await _context.users.AnyAsync(x => x.id == id);
            if (!exists)
                return NotFound(new { message = "User not found." });

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM users
                WHERE id = {id};
                """);

            return Ok(new { message = "User deleted successfully." });
        }
    }
}