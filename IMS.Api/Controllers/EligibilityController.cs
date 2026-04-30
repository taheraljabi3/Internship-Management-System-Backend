using IMS.Api.Data;
using IMS.Api.DTOs;
using IMS.Api.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EligibilityController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EligibilityController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("owner/{ownerUserId:long}")]
        public async Task<IActionResult> GetByOwner(long ownerUserId)
        {
            var sql = """
                SELECT
                    e.id,
                    e.student_user_id,
                    u.full_name AS student_name,
                    u.email AS student_email,
                    e.approval_owner_user_id,
                    e.approval_owner_role::text AS approval_owner_role,
                    e.status::text AS status,
                    e.comment,
                    e.created_at,
                    v.advisor_name
                FROM student_eligibility_reviews e
                JOIN users u ON u.id = e.student_user_id
                LEFT JOIN vw_student_current_advisor v ON v.student_user_id = e.student_user_id
                WHERE e.approval_owner_user_id = @ownerUserId
                ORDER BY e.created_at DESC
                """;

            var items = await _context.Database.SqlQueryRaw<EligibilityListItemDto>(
                    sql,
                    new NpgsqlParameter("ownerUserId", ownerUserId))
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("student/{studentUserId:long}")]
        public async Task<IActionResult> GetByStudent(long studentUserId)
        {
            var sql = """
                SELECT
                    e.id,
                    e.student_user_id,
                    u.full_name AS student_name,
                    u.email AS student_email,
                    e.approval_owner_user_id,
                    e.approval_owner_role::text AS approval_owner_role,
                    e.status::text AS status,
                    e.comment,
                    e.created_at,
                    NULL::text AS advisor_name
                FROM student_eligibility_reviews e
                JOIN users u ON u.id = e.student_user_id
                WHERE e.student_user_id = @studentUserId
                LIMIT 1
                """;

            var item = await _context.Database.SqlQueryRaw<EligibilityListItemDto>(
                    sql,
                    new NpgsqlParameter("studentUserId", studentUserId))
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = "Eligibility review not found." });

            return Ok(item);
        }

    [HttpPost]
    public async Task<IActionResult> CreateOrReplace(CreateEligibilityReviewRequest request)
    {
        if (!request.invitation_recipient_id.HasValue || request.invitation_recipient_id.Value <= 0)
            return BadRequest(new { message = "invitation_recipient_id is required." });

        var invitationInfo = await _context.Database.SqlQueryRaw<InvitationEligibilityLookupDto>(
                """
                SELECT
                    ir.id AS invitation_recipient_id,
                    ir.student_email,
                    ib.advisor_user_id
                FROM invitation_recipients ir
                JOIN invitation_batches ib
                    ON ib.id = ir.batch_id
                WHERE ir.id = @recipientId
                LIMIT 1
                """,
                new NpgsqlParameter("recipientId", request.invitation_recipient_id.Value))
            .FirstOrDefaultAsync();

        if (invitationInfo == null)
            return BadRequest(new { message = "Invitation recipient was not found." });

        if (string.IsNullOrWhiteSpace(invitationInfo.student_email))
            return BadRequest(new { message = "Invitation recipient email is missing." });

        var resolvedStudentUserId = await _context.Database.SqlQueryRaw<long>(
                """
                SELECT u.id AS "Value"
                FROM users u
                WHERE LOWER(u.email) = LOWER(@studentEmail)
                LIMIT 1
                """,
                new NpgsqlParameter("studentEmail", invitationInfo.student_email))
            .FirstOrDefaultAsync();

        if (resolvedStudentUserId <= 0)
            return BadRequest(new { message = "Student account has not been created yet from the invitation link." });

        var approvalOwnerUserId = request.approval_owner_user_id.HasValue && request.approval_owner_user_id.Value > 0
            ? request.approval_owner_user_id.Value
            : invitationInfo.advisor_user_id;

        if (approvalOwnerUserId <= 0)
            return BadRequest(new { message = "Approval owner could not be resolved." });

        var ownerExists = await _context.Database.SqlQueryRaw<long>(
                """
                SELECT id AS "Value"
                FROM users
                WHERE id = @ownerUserId
                LIMIT 1
                """,
                new NpgsqlParameter("ownerUserId", approvalOwnerUserId))
            .AnyAsync();

        if (!ownerExists)
            return BadRequest(new { message = "Approval owner was not found." });

        await using var tx = await _context.Database.BeginTransactionAsync();

        var existingId = await _context.Database.SqlQueryRaw<long>(
                """
                SELECT id AS "Value"
                FROM student_eligibility_reviews
                WHERE student_user_id = @studentUserId
                LIMIT 1
                """,
                new NpgsqlParameter("studentUserId", resolvedStudentUserId))
            .FirstOrDefaultAsync();

        long reviewId;

        if (existingId == 0)
        {
            reviewId = _context.Database.SqlQuery<long>($"""
                INSERT INTO student_eligibility_reviews
                    (
                        student_user_id,
                        invitation_recipient_id,
                        advisor_assignment_id,
                        approval_owner_user_id,
                        approval_owner_role,
                        status,
                        comment,
                        reviewer_user_id,
                        reviewed_at,
                        approved_at,
                        rejected_at,
                        created_at,
                        updated_at
                    )
                VALUES
                    (
                        {resolvedStudentUserId},
                        {request.invitation_recipient_id},
                        {request.advisor_assignment_id},
                        {approvalOwnerUserId},
                        {request.approval_owner_role}::approver_type_enum,
                        {"Pending"}::approval_status_enum,
                        {null},
                        {null},
                        {null},
                        {null},
                        {null},
                        NOW(),
                        NOW()
                    )
                RETURNING id AS "Value"
                """).AsEnumerable().Single();
        }
        else
        {
            reviewId = existingId;

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE student_eligibility_reviews
                SET
                    invitation_recipient_id = {request.invitation_recipient_id},
                    advisor_assignment_id = {request.advisor_assignment_id},
                    approval_owner_user_id = {approvalOwnerUserId},
                    approval_owner_role = {request.approval_owner_role}::approver_type_enum,
                    status = {"Pending"}::approval_status_enum,
                    comment = {null},
                    reviewer_user_id = {null},
                    reviewed_at = {null},
                    approved_at = {null},
                    rejected_at = {null},
                    updated_at = NOW()
                WHERE id = {reviewId}
                """);
        }

        await tx.CommitAsync();

        return Ok(new
        {
            id = reviewId,
            student_user_id = resolvedStudentUserId,
            message = "Eligibility review created successfully."
        });
    }


        [HttpPost("{id:long}/approve")]
        public async Task<IActionResult> Approve(long id, ReviewDecisionRequest request)
        {
            var review = await _context.student_eligibility_reviews
                .AsNoTracking()
                .Where(x => x.id == id)
                .Select(x => new { x.id, x.student_user_id })
                .FirstOrDefaultAsync();

            if (review == null)
                return NotFound(new { message = "Eligibility review not found." });

            var student = await _context.users
                .AsNoTracking()
                .Where(x => x.id == review.student_user_id)
                .Select(x => new { x.id, x.email, x.full_name })
                .FirstOrDefaultAsync();

            if (student == null)
                return NotFound(new { message = "Student not found." });

            await using var tx = await _context.Database.BeginTransactionAsync();

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE student_eligibility_reviews
                SET status = {"Approved"}::approval_status_enum,
                    comment = {request.comment},
                    reviewer_user_id = {request.actor_user_id},
                    reviewed_at = NOW(),
                    approved_at = NOW(),
                    rejected_at = NULL,
                    updated_at = NOW()
                WHERE id = {id}
                """);

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE users
                SET status = {"Active"}::user_status_enum,
                    updated_at = NOW()
                WHERE id = {review.student_user_id}
                  AND status = {"Pending"}::user_status_enum
                """);

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
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
                        {review.student_user_id},
                        {"Training Eligibility Letter"},
                        {$"training-letter-{review.student_user_id}.pdf"},
                        {null},
                        {"TrainingLetter"}::document_category_enum,
                        {"PDF"}::file_type_enum,
                        {"Generated"}::document_status_enum,
                        NOW(),
                        {"Auto-generated after eligibility approval."},
                        {request.actor_user_id}
                    )
                """);

            await WorkflowWriteHelper.LogApprovalActionAsync(
                _context,
                "StudentEligibility",
                id,
                review.student_user_id,
                request.actor_user_id,
                "Approved",
                request.comment);

            await WorkflowWriteHelper.QueueInAppAsync(
                _context,
                review.student_user_id,
                "Eligibility Approved",
                "Your training eligibility has been approved and the training letter was generated.",
                "student_eligibility_reviews",
                id);

            // await WorkflowWriteHelper.QueueEmailAsync(
            //     _context,
            //     student.email,
            //     "Training Eligibility Approved",
            //     "Your eligibility has been approved. Your stamped/signed training letter is now available in your student profile.",
            //     "student_eligibility_reviews",
            //     id);

            await tx.CommitAsync();

            return Ok(new { message = "Eligibility approved successfully." });
        }

        [HttpPost("{id:long}/reject")]
        public async Task<IActionResult> Reject(long id, ReviewDecisionRequest request)
        {
            var review = await _context.student_eligibility_reviews
                .AsNoTracking()
                .Where(x => x.id == id)
                .Select(x => new { x.id, x.student_user_id })
                .FirstOrDefaultAsync();

            if (review == null)
                return NotFound(new { message = "Eligibility review not found." });

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE student_eligibility_reviews
                SET status = {"Rejected"}::approval_status_enum,
                    comment = {request.comment},
                    reviewer_user_id = {request.actor_user_id},
                    reviewed_at = NOW(),
                    rejected_at = NOW(),
                    approved_at = NULL,
                    updated_at = NOW()
                WHERE id = {id}
                """);

            await WorkflowWriteHelper.LogApprovalActionAsync(
                _context,
                "StudentEligibility",
                id,
                review.student_user_id,
                request.actor_user_id,
                "Rejected",
                request.comment);

            await WorkflowWriteHelper.QueueInAppAsync(
                _context,
                review.student_user_id,
                "Eligibility Rejected",
                request.comment ?? "Your eligibility request was rejected.",
                "student_eligibility_reviews",
                id);

            return Ok(new { message = "Eligibility rejected successfully." });
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> GetPendingQueue()
        {
            var sql = """
                SELECT
                    ir.id AS invitation_recipient_id,
                    ir.batch_id AS invitation_batch_id,

                    er.id AS eligibility_review_id,

                    u.id AS student_user_id,
                    ir.student_name,
                    ir.student_email,

                    ib.advisor_user_id,
                    au.full_name AS advisor_name,
                    au.email AS advisor_email,

                    ir.invitation_status::text AS invitation_status,
                    COALESCE(er.status::text, 'Pending') AS eligibility_status,
                    u.status::text AS user_status,

                    CASE
                        WHEN u.id IS NULL THEN FALSE
                        ELSE TRUE
                    END AS account_created,

                    COALESCE(er.created_at, ir.created_at) AS queue_created_at
                FROM invitation_recipients ir
                JOIN invitation_batches ib
                    ON ib.id = ir.batch_id
                JOIN users au
                    ON au.id = ib.advisor_user_id
                LEFT JOIN users u
                    ON ir.student_email IS NOT NULL
                AND LOWER(u.email) = LOWER(ir.student_email)
                LEFT JOIN student_eligibility_reviews er
                    ON er.student_user_id = u.id
                WHERE ir.student_email IS NOT NULL
                AND ir.invitation_status = 'Accepted'::invitation_status_enum
                AND (er.id IS NULL OR er.status = 'Pending'::approval_status_enum)
                AND (u.id IS NULL OR u.status = 'Pending'::user_status_enum)
                ORDER BY COALESCE(er.created_at, ir.created_at) DESC, ir.id DESC
                """;

            var items = await _context.Database
                .SqlQueryRaw<PendingEligibilityQueueItemDto>(sql)
                .ToListAsync();

            return Ok(items);
        }


                
        [HttpGet("pending/me")]
        [Authorize(Roles = "AcademicAdvisor,Administrator")]
        public async Task<IActionResult> GetMyPendingQueue()
        {
            var ownerUserId = User.GetUserId();

            var sql = """
                SELECT
                    ir.id AS invitation_recipient_id,
                    ir.batch_id AS invitation_batch_id,

                    er.id AS eligibility_review_id,

                    u.id AS student_user_id,
                    ir.student_name,
                    ir.student_email,

                    ib.advisor_user_id,
                    au.full_name AS advisor_name,
                    au.email AS advisor_email,

                    ir.invitation_status::text AS invitation_status,
                    COALESCE(er.status::text, 'Pending') AS eligibility_status,
                    u.status::text AS user_status,

                    CASE
                        WHEN u.id IS NULL THEN FALSE
                        ELSE TRUE
                    END AS account_created,

                    COALESCE(er.created_at, ir.created_at) AS queue_created_at
                FROM invitation_recipients ir
                JOIN invitation_batches ib
                    ON ib.id = ir.batch_id
                JOIN users au
                    ON au.id = ib.advisor_user_id
                LEFT JOIN users u
                    ON ir.student_email IS NOT NULL
                AND LOWER(u.email) = LOWER(ir.student_email)
                LEFT JOIN student_eligibility_reviews er
                    ON er.student_user_id = u.id
                WHERE ir.student_email IS NOT NULL
                AND ir.invitation_status = 'Accepted'::invitation_status_enum
                AND (er.id IS NULL OR er.status = 'Pending'::approval_status_enum)
                AND (u.id IS NULL OR u.status = 'Pending'::user_status_enum)
                AND (
                        ib.advisor_user_id = @ownerUserId
                    OR er.approval_owner_user_id = @ownerUserId
                )
                ORDER BY COALESCE(er.created_at, ir.created_at) DESC, ir.id DESC
                """;

            var items = await _context.Database
                .SqlQueryRaw<PendingEligibilityQueueItemDto>(
                    sql,
                    new NpgsqlParameter("ownerUserId", ownerUserId))
                .ToListAsync();

            return Ok(items);
        }

    }
}