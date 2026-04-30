using IMS.Api.Data;
using IMS.Api.DTOs;
using IMS.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainingCompanyRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TrainingCompanyRequestsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("student/{studentUserId:long}")]
        public async Task<IActionResult> GetByStudent(long studentUserId)
        {
            var sql = """
                SELECT
                    r.id,
                    r.student_user_id,
                    su.full_name AS student_name,
                    su.email AS student_email,
                    r.provider_name,
                    r.provider_email,
                    r.contact_name,
                    r.contact_phone,
                    r.city,
                    r.sector,
                    r.opportunity_title,
                    r.approval_owner_user_id,
                    r.approval_owner_role::text AS approval_owner_role,
                    ou.full_name AS approval_owner_name,
                    r.assigned_advisor_user_id,
                    au.full_name AS assigned_advisor_name,
                    r.status::text AS status,
                    r.approval_comment,
                    r.submitted_at,
                    r.reviewed_at
                FROM training_company_requests r
                JOIN users su ON su.id = r.student_user_id
                JOIN users ou ON ou.id = r.approval_owner_user_id
                JOIN users au ON au.id = r.assigned_advisor_user_id
                WHERE r.student_user_id = @studentUserId
                ORDER BY r.submitted_at DESC;
                """;

            var items = await _context.Database.SqlQueryRaw<TrainingCompanyRequestListItemDto>(
                sql,
                new Npgsql.NpgsqlParameter("studentUserId", studentUserId))
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("owner/{ownerUserId:long}")]
        public async Task<IActionResult> GetByOwner(long ownerUserId)
        {
            var sql = """
                SELECT
                    r.id,
                    r.student_user_id,
                    su.full_name AS student_name,
                    su.email AS student_email,
                    r.provider_name,
                    r.provider_email,
                    r.contact_name,
                    r.contact_phone,
                    r.city,
                    r.sector,
                    r.opportunity_title,
                    r.approval_owner_user_id,
                    r.approval_owner_role::text AS approval_owner_role,
                    ou.full_name AS approval_owner_name,
                    r.assigned_advisor_user_id,
                    au.full_name AS assigned_advisor_name,
                    r.status::text AS status,
                    r.approval_comment,
                    r.submitted_at,
                    r.reviewed_at
                FROM training_company_requests r
                JOIN users su ON su.id = r.student_user_id
                JOIN users ou ON ou.id = r.approval_owner_user_id
                JOIN users au ON au.id = r.assigned_advisor_user_id
                WHERE r.approval_owner_user_id = @ownerUserId
                ORDER BY r.submitted_at DESC;
                """;

            var items = await _context.Database.SqlQueryRaw<TrainingCompanyRequestListItemDto>(
                sql,
                new Npgsql.NpgsqlParameter("ownerUserId", ownerUserId))
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateTrainingCompanyRequestDto request)
        {
            if (request.student_user_id <= 0)
                return BadRequest(new { message = "student_user_id is required." });

            var eligibilityApproved = await _context.Database.SqlQueryRaw<long>(
                    """
                    SELECT id AS "Value"
                    FROM student_eligibility_reviews
                    WHERE student_user_id = @studentUserId
                    AND status = 'Approved'::approval_status_enum
                    LIMIT 1
                    """,
                    new Npgsql.NpgsqlParameter("studentUserId", request.student_user_id))
                .AnyAsync();

            if (!eligibilityApproved)
                return BadRequest(new { message = "The student must be approved as eligible before submitting a training company." });
            var assignedAdvisorUserId = _context.Database.SqlQuery<long>($"""
                SELECT advisor_user_id AS "Value"
                FROM advisor_student_assignments
                WHERE student_user_id = {request.student_user_id}
                AND status = {"Active"}::assignment_status_enum
                ORDER BY assignment_start_at DESC, id DESC
                LIMIT 1
                """).AsEnumerable().FirstOrDefault();

            if (assignedAdvisorUserId <= 0)
                return BadRequest(new { message = "No active academic advisor is linked to this student." });

            var requestId = _context.Database.SqlQuery<long>($"""
                INSERT INTO training_company_requests
                    (
                        student_user_id,
                        assigned_advisor_user_id,
                        provider_name,
                        provider_email,
                        contact_name,
                        contact_phone,
                        city,
                        sector,
                        opportunity_title,
                        approval_owner_user_id,
                        approval_owner_role,
                        status,
                        submitted_at
                    )
                VALUES
                    (
                        {request.student_user_id},
                        {assignedAdvisorUserId},
                        {request.provider_name},
                        {request.provider_email},
                        {request.contact_name},
                        {request.contact_phone},
                        {request.city},
                        {request.sector},
                        {request.opportunity_title},
                        {assignedAdvisorUserId},
                        {"AcademicAdvisor"}::approver_type_enum,
                        {"Pending"}::approval_status_enum,
                        NOW()
                    )
                RETURNING id AS "Value"
                """).AsEnumerable().Single();
                
                        await WorkflowWriteHelper.QueueInAppAsync(
                            _context,
                            assignedAdvisorUserId,
                            "Training Company Approval Request",
                            "A new training company request has been submitted and requires your review.",
                            "training_company_requests",
                            requestId);

                        return Ok(new
                        {
                            id = requestId,
                            message = "Training company request submitted successfully."
                        });
                    }
 
[HttpPost("{id:long}/approve")]
public async Task<IActionResult> Approve(long id, ReviewDecisionRequest request)
{
    var companyRequest = await _context.training_company_requests
        .AsNoTracking()
        .Where(x => x.id == id)
        .Select(x => new
        {
            x.id,
            x.student_user_id,
            x.assigned_advisor_user_id,
            x.provider_name,
            x.provider_email,
            x.opportunity_title
        })
        .FirstOrDefaultAsync();

    if (companyRequest == null)
        return NotFound(new { message = "Training company request not found." });

    await using var tx = await _context.Database.BeginTransactionAsync();

    await _context.Database.ExecuteSqlInterpolatedAsync($"""
        UPDATE training_company_requests
        SET status = {"Approved"}::approval_status_enum,
            approval_comment = {request.comment},
            reviewer_user_id = {request.actor_user_id},
            reviewed_at = NOW(),
            approved_at = NOW(),
            rejected_at = NULL,
            updated_at = NOW()
        WHERE id = {id}
        """);

    var existingInternshipId = _context.Database.SqlQuery<long>($"""
        SELECT id AS "Value"
        FROM internships
        WHERE company_request_id = {id}
        LIMIT 1
        """).AsEnumerable().FirstOrDefault();

    long internshipId = existingInternshipId;

    if (internshipId <= 0)
    {
        internshipId = _context.Database.SqlQuery<long>($"""
            INSERT INTO internships
                (
                    student_user_id,
                    company_request_id,
                    provider_name,
                    provider_email,
                    internship_title,
                    status,
                    approved_by_academic_advisor,
                    created_at,
                    updated_at
                )
            VALUES
                (
                    {companyRequest.student_user_id},
                    {id},
                    {companyRequest.provider_name},
                    {companyRequest.provider_email},
                    {companyRequest.opportunity_title},
                    {"Approved"},
                    TRUE,
                    NOW(),
                    NOW()
                )
            RETURNING id AS "Value"
            """).AsEnumerable().Single();
    }

    await WorkflowWriteHelper.LogApprovalActionAsync(
        _context,
        "TrainingCompanyRequest",
        id,
        companyRequest.student_user_id,
        request.actor_user_id,
        "Approved",
        request.comment);

    await WorkflowWriteHelper.QueueInAppAsync(
        _context,
        companyRequest.student_user_id,
        "Training Company Approved",
        $"Your training company request for {companyRequest.provider_name} has been approved.",
        "training_company_requests",
        id);

    await tx.CommitAsync();

    return Ok(new
    {
        internship_id = internshipId,
        message = "Training company request approved successfully."
    });
}


        [HttpPost("{id:long}/reject")]
        public async Task<IActionResult> Reject(long id, ReviewDecisionRequest request)
        {
            var companyRequest = await _context.training_company_requests
                .AsNoTracking()
                .Where(x => x.id == id)
                .Select(x => new
                {
                    x.id,
                    x.student_user_id,
                    x.provider_name
                })
                .FirstOrDefaultAsync();

            if (companyRequest == null)
                return NotFound(new { message = "Training company request not found." });

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE training_company_requests
                SET status = {"Rejected"}::approval_status_enum,
                    approval_comment = {request.comment},
                    reviewer_user_id = {request.actor_user_id},
                    reviewed_at = NOW(),
                    rejected_at = NOW(),
                    approved_at = NULL,
                    updated_at = NOW()
                WHERE id = {id}
                """);

            await WorkflowWriteHelper.LogApprovalActionAsync(
                _context,
                "TrainingCompanyRequest",
                id,
                companyRequest.student_user_id,
                request.actor_user_id,
                "Rejected",
                request.comment);

            await WorkflowWriteHelper.QueueInAppAsync(
                _context,
                companyRequest.student_user_id,
                "Training Company Rejected",
                request.comment ?? "Your training company request was rejected.",
                "training_company_requests",
                id);

            return Ok(new { message = "Training company request rejected successfully." });
        }

        [HttpPost("{id:long}/delegate")]
        public async Task<IActionResult> Delegate(long id, DelegateApprovalRequest request)
        {
            var sql = """
                SELECT
                    id,
                    student_user_id,
                    approval_owner_user_id,
                    approval_owner_role::text AS owner_role
                FROM training_company_requests
                WHERE id = @id
                LIMIT 1;
                """;

            var item = await _context.Database.SqlQueryRaw<TrainingCompanyRequestDelegateRow>(
                sql,
                new Npgsql.NpgsqlParameter("id", id))
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = "Training company request not found." });

            await using var tx = await _context.Database.BeginTransactionAsync();

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE training_company_requests
                SET approval_owner_user_id = {request.to_owner_user_id},
                    approval_owner_role = {request.to_owner_role}::approver_type_enum,
                    updated_at = NOW()
                WHERE id = {id};
                """);

            await WorkflowWriteHelper.LogDelegationAsync(_context, "TrainingCompanyRequest", id, item.student_user_id, item.approval_owner_user_id, request.to_owner_user_id, item.owner_role, request.to_owner_role, request.reason, request.changed_by_user_id);
            await tx.CommitAsync();

            return Ok(new { message = "Approval owner changed successfully." });
        }

        private class TrainingCompanyRequestDelegateRow
        {
            public long id { get; set; }
            public long student_user_id { get; set; }
            public long approval_owner_user_id { get; set; }
            public string owner_role { get; set; } = string.Empty;
        }
    }
}