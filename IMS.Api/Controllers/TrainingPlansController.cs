using System.IO;
using IMS.Api.Data;
using IMS.Api.DTOs;
using IMS.Api.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrainingPlansController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TrainingPlansController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("internship/{internshipId:long}")]
        public async Task<IActionResult> GetByInternship(long internshipId)
        {
            var sql = """
                SELECT
                    p.id,
                    p.internship_id,
                    p.student_user_id,
                    su.full_name AS student_name,
                    i.provider_name,
                    i.internship_title,
                    p.start_date,
                    p.plan_title,
                    p.plan_summary,
                    p.approval_owner_user_id,
                    p.approval_owner_role::text AS approval_owner_role,
                    ou.full_name AS approval_owner_name,
                    p.assigned_advisor_user_id,
                    au.full_name AS assigned_advisor_name,
                    p.status::text AS status,
                    p.approval_comment,
                    p.submitted_at,
                    p.reviewed_at
                FROM training_plans p
                JOIN internships i ON i.id = p.internship_id
                JOIN users su ON su.id = p.student_user_id
                JOIN users ou ON ou.id = p.approval_owner_user_id
                JOIN users au ON au.id = p.assigned_advisor_user_id
                WHERE p.internship_id = @internshipId
                ORDER BY p.submitted_at DESC
                """;

            var items = await _context.Database.SqlQueryRaw<TrainingPlanListItemDto>(
                    sql,
                    new NpgsqlParameter("internshipId", internshipId))
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("owner/{ownerUserId:long}")]
        public async Task<IActionResult> GetByOwner(long ownerUserId)
        {
            var sql = """
                SELECT
                    p.id,
                    p.internship_id,
                    p.student_user_id,
                    su.full_name AS student_name,
                    i.provider_name,
                    i.internship_title,
                    p.start_date,
                    p.plan_title,
                    p.plan_summary,
                    p.approval_owner_user_id,
                    p.approval_owner_role::text AS approval_owner_role,
                    ou.full_name AS approval_owner_name,
                    p.assigned_advisor_user_id,
                    au.full_name AS assigned_advisor_name,
                    p.status::text AS status,
                    p.approval_comment,
                    p.submitted_at,
                    p.reviewed_at,
                    p.approved_at
                FROM training_plans p
                JOIN internships i ON i.id = p.internship_id
                JOIN users su ON su.id = p.student_user_id
                JOIN users ou ON ou.id = p.approval_owner_user_id
                JOIN users au ON au.id = p.assigned_advisor_user_id
                WHERE p.approval_owner_user_id = @ownerUserId
                ORDER BY p.submitted_at DESC
                """;

            var items = await _context.Database.SqlQueryRaw<TrainingPlanListItemDto>(
                    sql,
                    new NpgsqlParameter("ownerUserId", ownerUserId))
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateTrainingPlanRequestDto request)
        {
            if (request.internship_id <= 0)
                return BadRequest(new { message = "internship_id is required." });

            if (request.student_user_id <= 0)
                return BadRequest(new { message = "student_user_id is required." });

            if (string.IsNullOrWhiteSpace(request.plan_title))
                return BadRequest(new { message = "plan_title is required." });

            if (string.IsNullOrWhiteSpace(request.plan_summary))
                return BadRequest(new { message = "plan_summary is required." });

            var internshipInfo = await _context.Database
                .SqlQueryRaw<TrainingPlanInternshipLookupDto>(
                    """
                    SELECT
                        i.id AS internship_id,
                        i.student_user_id,
                        i.company_request_id,
                        i.provider_name
                    FROM internships i
                    WHERE i.id = @internshipId
                      AND i.student_user_id = @studentUserId
                    LIMIT 1
                    """,
                    new NpgsqlParameter("internshipId", request.internship_id),
                    new NpgsqlParameter("studentUserId", request.student_user_id))
                .FirstOrDefaultAsync();

            if (internshipInfo == null)
                return BadRequest(new { message = "Internship was not found for this student." });

            var approvedCompanyRequestId = request.company_request_id ?? internshipInfo.company_request_id;

            if (!approvedCompanyRequestId.HasValue || approvedCompanyRequestId.Value <= 0)
                return BadRequest(new { message = "Approved training company is required before creating the training plan." });

            var approvedCompany = await _context.Database
                .SqlQueryRaw<TrainingPlanCompanyLookupDto>(
                    """
                    SELECT
                        tcr.id,
                        tcr.student_user_id,
                        tcr.provider_name,
                        tcr.status::text AS status,
                        tcr.assigned_advisor_user_id,
                        tcr.approval_owner_user_id
                    FROM training_company_requests tcr
                    WHERE tcr.id = @companyRequestId
                      AND tcr.student_user_id = @studentUserId
                    LIMIT 1
                    """,
                    new NpgsqlParameter("companyRequestId", approvedCompanyRequestId.Value),
                    new NpgsqlParameter("studentUserId", request.student_user_id))
                .FirstOrDefaultAsync();

            if (approvedCompany == null)
                return BadRequest(new { message = "Approved training company request was not found." });

            if (approvedCompany.status != "Approved")
                return BadRequest(new { message = "Training plan can be submitted only after the training company is approved." });

            var assignedAdvisorUserId = approvedCompany.assigned_advisor_user_id;
            var approvalOwnerUserId = approvedCompany.approval_owner_user_id;

            if (assignedAdvisorUserId <= 0 || approvalOwnerUserId <= 0)
                return BadRequest(new { message = "Advisor / approval owner could not be resolved for this training plan." });

            var acceptedPlatform = !string.IsNullOrWhiteSpace(request.accepted_platform)
                ? request.accepted_platform!.Trim()
                : approvedCompany.provider_name;

            var finalPlanSummary = $"Accepted Platform: {acceptedPlatform}\n\n{request.plan_summary}";

            await using var tx = await _context.Database.BeginTransactionAsync();

            var trainingPlanId = _context.Database.SqlQuery<long>($"""
                INSERT INTO training_plans
                    (
                        internship_id,
                        student_user_id,
                        start_date,
                        plan_title,
                        plan_summary,
                        approval_owner_user_id,
                        approval_owner_role,
                        assigned_advisor_user_id,
                        status,
                        submitted_at,
                        created_at,
                        updated_at
                    )
                VALUES
                    (
                        {request.internship_id},
                        {request.student_user_id},
                        {request.start_date},
                        {request.plan_title},
                        {finalPlanSummary},
                        {approvalOwnerUserId},
                        {"AcademicAdvisor"}::approver_type_enum,
                        {assignedAdvisorUserId},
                        {"Pending"}::approval_status_enum,
                        NOW(),
                        NOW(),
                        NOW()
                    )
                RETURNING id AS "Value"
                """).AsEnumerable().Single();

            if (!string.IsNullOrWhiteSpace(request.attachment_file_name))
            {
                var extension = Path.GetExtension(request.attachment_file_name)?
                    .TrimStart('.')
                    .ToUpperInvariant() ?? "OTHER";

                var fileTypeEnum = extension switch
                {
                    "PDF" => "PDF",
                    "DOCX" => "DOCX",
                    "PPTX" => "PPTX",
                    "PNG" => "PNG",
                    "JPG" => "JPG",
                    "JPEG" => "JPEG",
                    _ => "OTHER"
                };

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
                            {request.student_user_id},
                            {$"Training Plan Attachment - {request.plan_title}"},
                            {request.attachment_file_name},
                            {request.attachment_file_url},
                            {"OtherAttachment"}::document_category_enum,
                            {fileTypeEnum}::file_type_enum,
                            {"Uploaded"}::document_status_enum,
                            NOW(),
                            {$"Attachment for training plan #{trainingPlanId}"},
                            {request.student_user_id}
                        )
                    """);
            }

            await WorkflowWriteHelper.LogApprovalActionAsync(
                _context,
                "TrainingPlan",
                trainingPlanId,
                request.student_user_id,
                request.student_user_id,
                "Pending",
                "Training plan submitted by student."
            );

            await WorkflowWriteHelper.QueueInAppAsync(
                _context,
                approvalOwnerUserId,
                "Training Plan Approval Request",
                $"A new training plan has been submitted and requires your review: {request.plan_title}",
                "training_plans",
                trainingPlanId
            );

            await tx.CommitAsync();

            return Ok(new
            {
                id = trainingPlanId,
                message = "Training plan submitted successfully."
            });
        }

        [HttpPost("{id:long}/approve")]
        public async Task<IActionResult> Approve(long id, ReviewDecisionRequest request)
        {
            var item = await _context.training_plans
                .AsNoTracking()
                .Where(x => x.id == id)
                .Select(x => new { x.id, x.student_user_id })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = "Training plan not found." });

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE training_plans
                SET status = {"Approved"}::approval_status_enum,
                    approval_comment = {request.comment},
                    reviewer_user_id = {request.actor_user_id},
                    reviewed_at = NOW(),
                    approved_at = NOW(),
                    updated_at = NOW()
                WHERE id = {id}
                """);

            await WorkflowWriteHelper.LogApprovalActionAsync(
                _context,
                "TrainingPlan",
                id,
                item.student_user_id,
                request.actor_user_id,
                "Approved",
                request.comment
            );

            await WorkflowWriteHelper.QueueInAppAsync(
                _context,
                item.student_user_id,
                "Training Plan Approved",
                "Your training plan was approved. You can now add daily tasks and evidence.",
                "training_plans",
                id
            );

            return Ok(new { message = "Training plan approved successfully." });
        }

        [HttpPost("{id:long}/reject")]
        public async Task<IActionResult> Reject(long id, ReviewDecisionRequest request)
        {
            var item = await _context.training_plans
                .AsNoTracking()
                .Where(x => x.id == id)
                .Select(x => new { x.id, x.student_user_id })
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = "Training plan not found." });

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE training_plans
                SET status = {"Rejected"}::approval_status_enum,
                    approval_comment = {request.comment},
                    reviewer_user_id = {request.actor_user_id},
                    reviewed_at = NOW(),
                    rejected_at = NOW(),
                    updated_at = NOW()
                WHERE id = {id}
                """);

            await WorkflowWriteHelper.LogApprovalActionAsync(
                _context,
                "TrainingPlan",
                id,
                item.student_user_id,
                request.actor_user_id,
                "Rejected",
                request.comment
            );

            await WorkflowWriteHelper.QueueInAppAsync(
                _context,
                item.student_user_id,
                "Training Plan Rejected",
                request.comment ?? "The training plan was rejected.",
                "training_plans",
                id
            );

            return Ok(new { message = "Training plan rejected successfully." });
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
                FROM training_plans
                WHERE id = @id
                LIMIT 1
                """;

            var item = await _context.Database.SqlQueryRaw<TrainingPlanDelegateRow>(
                    sql,
                    new NpgsqlParameter("id", id))
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = "Training plan not found." });

            await using var tx = await _context.Database.BeginTransactionAsync();

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE training_plans
                SET approval_owner_user_id = {request.to_owner_user_id},
                    approval_owner_role = {request.to_owner_role}::approver_type_enum,
                    updated_at = NOW()
                WHERE id = {id}
                """);

            await WorkflowWriteHelper.LogDelegationAsync(
                _context,
                "TrainingPlan",
                id,
                item.student_user_id,
                item.approval_owner_user_id,
                request.to_owner_user_id,
                item.owner_role,
                request.to_owner_role,
                request.reason,
                request.changed_by_user_id
            );

            await tx.CommitAsync();

            return Ok(new { message = "Plan approval owner changed successfully." });
        }

        private class TrainingPlanDelegateRow
        {
            public long id { get; set; }
            public long student_user_id { get; set; }
            public long approval_owner_user_id { get; set; }
            public string owner_role { get; set; } = string.Empty;
        }

        private class TrainingPlanInternshipLookupDto
        {
            public long internship_id { get; set; }
            public long student_user_id { get; set; }
            public long? company_request_id { get; set; }
            public string provider_name { get; set; } = string.Empty;
        }

        private class TrainingPlanCompanyLookupDto
        {
            public long id { get; set; }
            public long student_user_id { get; set; }
            public string provider_name { get; set; } = string.Empty;
            public string status { get; set; } = string.Empty;
            public long assigned_advisor_user_id { get; set; }
            public long approval_owner_user_id { get; set; }
        }
    }
}