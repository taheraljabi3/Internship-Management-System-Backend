using IMS.Api.Data;
using IMS.Api.DTOs;
using IMS.Api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class InvitationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public InvitationsController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("batches")]
        public async Task<IActionResult> GetBatches()
        {
            var sql = """
                SELECT
                    b.id,
                    b.invitation_mode::text AS invitation_mode,
                    b.advisor_user_id,
                    u.full_name AS advisor_name,
                    b.excel_file_name,
                    b.shared_link_url,
                    b.total_recipients,
                    b.sent_at,
                    b.created_at
                FROM invitation_batches b
                JOIN users u ON u.id = b.advisor_user_id
                ORDER BY b.created_at DESC
                """;

            var items = await _context.Database
                .SqlQueryRaw<InvitationBatchListItemDto>(sql)
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("batches/{batchId:long}/recipients")]
        public async Task<IActionResult> GetRecipients(long batchId)
        {
            var sql = """
                SELECT
                    id,
                    batch_id,
                    student_name,
                    student_email,
                    student_user_id,
                    invitation_status::text AS invitation_status,
                    sent_at,
                    accepted_at
                FROM invitation_recipients
                WHERE batch_id = @batchId
                ORDER BY student_name
                """;

            var items = await _context.Database
                .SqlQueryRaw<InvitationRecipientListItemDto>(
                    sql,
                    new NpgsqlParameter("batchId", batchId))
                .ToListAsync();

            return Ok(items);
        }

[HttpPost("batches")]
public async Task<IActionResult> CreateBatch(CreateInvitationBatchRequest request)
{
    var currentUserIdRaw =
        User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
        User.FindFirst("sub")?.Value;

    if (!long.TryParse(currentUserIdRaw, out var currentUserId))
        return Unauthorized(new { message = "Invalid user session." });

    var resolvedAdvisorUserId = request.advisor_user_id;

    if (User.IsInRole("AcademicAdvisor"))
    {
        resolvedAdvisorUserId = currentUserId;
    }

    var advisorExists = await _context.academic_advisor_profiles
        .AnyAsync(x => x.user_id == resolvedAdvisorUserId);

    if (!advisorExists)
        return BadRequest(new { message = "Academic advisor must be valid before creating invitations." });

    if (request.invitation_mode != "Excel" && request.invitation_mode != "Link")
        return BadRequest(new { message = "invitation_mode must be Excel or Link." });

    if (request.recipients == null)
        request.recipients = new List<InvitationRecipientInputDto>();

    if (request.invitation_mode == "Link" && string.IsNullOrWhiteSpace(request.shared_link_token))
        request.shared_link_token = Guid.NewGuid().ToString("N");

    await using var tx = await _context.Database.BeginTransactionAsync();

    var batchId = _context.Database.SqlQuery<long>($"""
        INSERT INTO invitation_batches
            (
                invitation_mode,
                advisor_user_id,
                created_by_user_id,
                excel_file_name,
                shared_link_token,
                shared_link_url,
                invitation_message,
                total_recipients,
                sent_at,
                created_at
            )
        VALUES
            (
                {request.invitation_mode}::invitation_mode_enum,
                {resolvedAdvisorUserId},
                {currentUserId},
                {request.excel_file_name},
                {request.shared_link_token},
                {request.shared_link_url},
                {request.invitation_message},
                {request.recipients.Count},
                NOW(),
                NOW()
            )
        RETURNING id AS "Value"
        """).AsEnumerable().Single();

    var frontendBaseUrl = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');

    var sharedLinkUrl = request.invitation_mode == "Link"
        ? $"{frontendBaseUrl}/register/invitation?batchId={batchId}"
        : request.shared_link_url;

    var sharedLinkToken = request.shared_link_token;

    await _context.Database.ExecuteSqlInterpolatedAsync($"""
        UPDATE invitation_batches
        SET shared_link_token = {sharedLinkToken},
            shared_link_url = {sharedLinkUrl}
        WHERE id = {batchId}
        """);

    var recipients = request.recipients
        .Where(x => !string.IsNullOrWhiteSpace(x.student_name))
        .GroupBy(x => (x.student_email ?? string.Empty).Trim().ToLower())
        .Select(g => g.First())
        .ToList();

    foreach (var recipient in recipients)
    {
        var recipientId = _context.Database.SqlQuery<long>($"""
            INSERT INTO invitation_recipients
                (
                    batch_id,
                    advisor_user_id,
                    student_name,
                    student_email,
                    invitation_status,
                    sent_at,
                    created_at
                )
            VALUES
                (
                    {batchId},
                    {resolvedAdvisorUserId},
                    {recipient.student_name},
                    {recipient.student_email},
                    {"Sent"}::invitation_status_enum,
                    NOW(),
                    NOW()
                )
            RETURNING id AS "Value"
            """).AsEnumerable().Single();

        if (!string.IsNullOrWhiteSpace(recipient.student_email))
        {
            var emailBody = request.invitation_mode == "Link"
                ? $"{request.invitation_message}\n\nRegistration Link:\n{sharedLinkUrl}"
                : request.invitation_message;

            await WorkflowWriteHelper.QueueEmailAsync(
                _context,
                recipient.student_email!,
                "Internship Platform Invitation",
                emailBody,
                "invitation_recipients",
                recipientId);
        }
    }

    await WorkflowWriteHelper.LogAuditAsync(
        _context,
        currentUserId,
        "Create Invitation Batch",
        "invitation_batches",
        batchId.ToString());

    await tx.CommitAsync();

    return Ok(new
    {
        id = batchId,
        shared_link_url = sharedLinkUrl,
        message = "Invitation batch created successfully."
    });
}
        [AllowAnonymous]
        [HttpPost("register-from-link")]
        public async Task<IActionResult> RegisterFromLink(RegisterFromInvitationLinkRequest request)
        {
            if (request.batch_id <= 0)
                return BadRequest(new { message = "batch_id is required." });

            if (string.IsNullOrWhiteSpace(request.full_name))
                return BadRequest(new { message = "full_name is required." });

            if (string.IsNullOrWhiteSpace(request.email))
                return BadRequest(new { message = "email is required." });

            if (string.IsNullOrWhiteSpace(request.password))
                return BadRequest(new { message = "password is required." });

            var batch = await _context.Database
                .SqlQueryRaw<InvitationBatchLookupDto>(
                    """
                    SELECT
                        id,
                        advisor_user_id,
                        invitation_mode::text AS invitation_mode
                    FROM invitation_batches
                    WHERE id = @batchId
                    LIMIT 1
                    """,
                    new NpgsqlParameter("batchId", request.batch_id))
                .FirstOrDefaultAsync();

            if (batch == null)
                return BadRequest(new { message = "Invitation batch not found." });

            var existingUserEmail = await _context.Database
                .SqlQueryRaw<long>(
                    """
                    SELECT id AS "Value"
                    FROM users
                    WHERE LOWER(email) = LOWER(@email)
                    LIMIT 1
                    """,
                    new NpgsqlParameter("email", request.email))
                .FirstOrDefaultAsync();

            if (existingUserEmail > 0)
                return BadRequest(new { message = "An account with this email already exists." });

            await using var tx = await _context.Database.BeginTransactionAsync();

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.password);

            var userId = _context.Database.SqlQuery<long>($"""
                INSERT INTO users
                    (
                        full_name,
                        email,
                        password_hash,
                        status,
                        created_at,
                        updated_at
                    )
                VALUES
                    (
                        {request.full_name},
                        {request.email},
                        {passwordHash},
                        {"Pending"}::user_status_enum,
                        NOW(),
                        NOW()
                    )
                RETURNING id AS "Value"
                """).AsEnumerable().Single();

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
                        {null},
                        {null},
                        {null},
                        NOW(),
                        NOW()
                    )
                """);

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO user_roles (user_id, role_id)
                SELECT {userId}, r.id
                FROM roles r
                WHERE r.code = {"Student"}
                """);

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE advisor_student_assignments
                SET status = {"Inactive"}::assignment_status_enum,
                    assignment_end_at = NOW(),
                    updated_at = NOW()
                WHERE student_user_id = {userId}
                  AND status = {"Active"}::assignment_status_enum
                """);

            var assignmentId = _context.Database.SqlQuery<long>($"""
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
                        {batch.advisor_user_id},
                        {batch.advisor_user_id},
                        {"Active"}::assignment_status_enum,
                        NOW(),
                        {"Assigned automatically from invitation link registration"},
                        NOW(),
                        NOW()
                    )
                RETURNING id AS "Value"
                """).AsEnumerable().Single();

            var existingRecipientId = await _context.Database
                .SqlQueryRaw<long>(
                    """
                    SELECT id AS "Value"
                    FROM invitation_recipients
                    WHERE batch_id = @batchId
                      AND student_email IS NOT NULL
                      AND LOWER(student_email) = LOWER(@email)
                    LIMIT 1
                    """,
                    new NpgsqlParameter("batchId", request.batch_id),
                    new NpgsqlParameter("email", request.email))
                .FirstOrDefaultAsync();

            long recipientId;

            if (existingRecipientId > 0)
            {
                recipientId = existingRecipientId;

                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE invitation_recipients
                    SET student_name = {request.full_name},
                        student_user_id = {userId},
                        invitation_status = {"Accepted"}::invitation_status_enum,
                        accepted_at = NOW()
                    WHERE id = {recipientId}
                    """);
            }
            else
            {
                recipientId = _context.Database.SqlQuery<long>($"""
                    INSERT INTO invitation_recipients
                        (
                            batch_id,
                            advisor_user_id,
                            student_name,
                            student_email,
                            student_user_id,
                            invitation_status,
                            sent_at,
                            accepted_at,
                            created_at
                        )
                    VALUES
                        (
                            {request.batch_id},
                            {batch.advisor_user_id},
                            {request.full_name},
                            {request.email},
                            {userId},
                            {"Accepted"}::invitation_status_enum,
                            NOW(),
                            NOW(),
                            NOW()
                        )
                    RETURNING id AS "Value"
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
                        status,
                        comment,
                        created_at,
                        updated_at
                    )
                VALUES
                    (
                        {userId},
                        {recipientId},
                        {assignmentId},
                        {batch.advisor_user_id},
                        {"AcademicAdvisor"}::approver_type_enum,
                        {"Pending"}::approval_status_enum,
                        {"Created automatically after invitation link registration"},
                        NOW(),
                        NOW()
                    )
                """);

            await WorkflowWriteHelper.QueueInAppAsync(
                _context,
                batch.advisor_user_id,
                "New Student Waiting for Eligibility",
                $"{request.full_name} created an account from the invitation link and is now waiting for eligibility review.",
                "student_eligibility_reviews",
                userId);

            // await WorkflowWriteHelper.QueueEmailAsync(
            //     _context,
            //     request.email,
            //     "Internship Account Created Successfully",
            //     "Your account has been created successfully and moved to the pending eligibility queue.",
            //     "users",
            //     userId);

            await tx.CommitAsync();

            return Ok(new
            {
                user_id = userId,
                message = "Account created successfully and moved to pending eligibility queue."
            });
        }
    }

    public class InvitationBatchLookupDto
    {
        public long id { get; set; }
        public long advisor_user_id { get; set; }
        public string invitation_mode { get; set; } = string.Empty;
    }

    public class RegisterFromInvitationLinkRequest
    {
        public long batch_id { get; set; }
        public string full_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
    }
}