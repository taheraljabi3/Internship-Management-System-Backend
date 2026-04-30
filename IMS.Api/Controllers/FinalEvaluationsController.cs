using System.Data;
using System.Globalization;
using IMS.Api.Data;
using IMS.Api.DTOs;
using IMS.Api.Helpers;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FinalEvaluationsController : ControllerBase
    {
        private const string BrevoSmtpHost = "smtp-relay.brevo.com";
        private const int BrevoSmtpPort = 587;
        private const string BrevoSmtpLogin = "a940e1001@smtp-brevo.com";
        private const string BrevoSmtpPassword = "h95O0D8HWpAF4kZI";
        private const string BrevoFromEmail = "taheraljabi3@gmail.com";
        private const string BrevoFromName = "Taher AL-Jabi";
        private const string BrevoReplyToEmail = "taheraljabi3@gmail.com";
        private const string BrevoReplyToName = "Taher AL-Jabi";

        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public FinalEvaluationsController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("requests/internship/{internshipId:long}")]
        public async Task<IActionResult> GetRequests(long internshipId)
        {
            var sql = """
                SELECT
                    r.id,
                    r.internship_id,
                    r.student_user_id,
                    su.full_name AS student_name,
                    r.provider_name,
                    r.provider_email,
                    r.sending_template_name,
                    r.evaluation_template_name,
                    r.company_evaluation_template_id,
                    r.public_token,
                    r.requested_by_user_id,
                    ru.full_name AS requested_by_name,
                    r.status::text AS status,
                    r.requested_at,
                    r.completed_at
                FROM final_evaluation_requests r
                JOIN users su ON su.id = r.student_user_id
                JOIN users ru ON ru.id = r.requested_by_user_id
                WHERE r.internship_id = @internshipId
                ORDER BY r.requested_at DESC
                """;

            var items = await _context.Database.SqlQueryRaw<FinalEvaluationRequestRow>(
                sql,
                new Npgsql.NpgsqlParameter("internshipId", internshipId))
                .ToListAsync();

            var frontendBaseUrl = GetFrontendBaseUrl();

            var result = items.Select(item =>
            {
                var isCompleted =
                    string.Equals(item.status, "Approved", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.status, "Submitted", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(item.status, "Completed", StringComparison.OrdinalIgnoreCase);

                return new
                {
                    item.id,
                    item.internship_id,
                    item.student_user_id,
                    item.student_name,
                    item.provider_name,
                    item.provider_email,
                    item.sending_template_name,
                    item.evaluation_template_name,
                    item.company_evaluation_template_id,
                    item.public_token,
                    registration_link = !isCompleted && item.public_token.HasValue
                        ? $"{frontendBaseUrl}/external/company-evaluation/register/{item.public_token}"
                        : null,
                    item.requested_by_user_id,
                    item.requested_by_name,
                    item.status,
                    item.requested_at,
                    item.completed_at
                };
            });

            return Ok(result);
        }

        [HttpGet("templates/internship/{internshipId:long}")]
        public async Task<IActionResult> GetTemplates(long internshipId)
        {
            var sql = """
                SELECT
                    id,
                    internship_id,
                    student_user_id,
                    provider_name,
                    provider_email,
                    title,
                    version,
                    status::text AS status,
                    token,
                    created_by_user_id,
                    created_at
                FROM company_evaluation_templates
                WHERE internship_id = @internshipId
                ORDER BY created_at DESC
                """;

            var items = await _context.Database.SqlQueryRaw<CompanyEvaluationTemplateDto>(
                sql,
                new Npgsql.NpgsqlParameter("internshipId", internshipId))
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("templates/{templateId:long}/criteria")]
        public async Task<IActionResult> GetTemplateCriteria(long templateId)
        {
            var items = await _context.company_evaluation_template_criteria
                .AsNoTracking()
                .Where(x => x.template_id == templateId)
                .OrderBy(x => x.sort_order)
                .Select(x => new CompanyEvaluationTemplateCriterionDto
                {
                    id = x.id,
                    template_id = x.template_id,
                    criterion_name = x.criterion_name,
                    weight = x.weight,
                    sort_order = x.sort_order
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("templates")]
        public async Task<IActionResult> CreateTemplate(CreateCompanyEvaluationTemplateRequest request)
        {
            if (request.criteria == null || request.criteria.Count == 0)
                return BadRequest(new { message = "At least one evaluation criterion is required." });

            if (request.internship_id <= 0)
                return BadRequest(new { message = "internship_id is required." });

            if (request.student_user_id <= 0)
                return BadRequest(new { message = "student_user_id is required." });

            await using var tx = await _context.Database.BeginTransactionAsync();

            var token = Guid.NewGuid().ToString("N");

            var templateId = await ExecuteScalarLongAsync($"""
                INSERT INTO company_evaluation_templates
                    (
                        internship_id,
                        student_user_id,
                        provider_name,
                        provider_email,
                        title,
                        version,
                        status,
                        token,
                        created_by_user_id,
                        created_at,
                        updated_at
                    )
                VALUES
                    (
                        {request.internship_id},
                        {request.student_user_id},
                        {request.provider_name},
                        {request.provider_email},
                        {request.title},
                        {request.version},
                        {"Active"}::evaluation_template_status_enum,
                        {token},
                        {request.created_by_user_id},
                        NOW(),
                        NOW()
                    )
                RETURNING id
                """);

            foreach (var criterion in request.criteria.OrderBy(x => x.sort_order))
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO company_evaluation_template_criteria
                        (template_id, criterion_name, weight, sort_order)
                    VALUES
                        ({templateId}, {criterion.criterion_name}, {criterion.weight}, {criterion.sort_order})
                    """);
            }

            await tx.CommitAsync();

            return Ok(new
            {
                id = templateId,
                token,
                message = "Company evaluation template created successfully."
            });
        }

        [HttpPost("requests")]
        public async Task<IActionResult> CreateRequest(FinalEvaluationRequestDto request)
        {
            if (request.internship_id <= 0)
                return BadRequest(new { message = "internship_id is required." });

            if (request.student_user_id <= 0)
                return BadRequest(new { message = "student_user_id is required." });

            if (request.requested_by_user_id <= 0)
                return BadRequest(new { message = "requested_by_user_id is required." });

            if (string.IsNullOrWhiteSpace(request.provider_email))
                return BadRequest(new { message = "provider_email is required to send the company evaluation request." });

            var normalizedProviderEmail = request.provider_email.Trim().ToLower();

            var templateId = TryGetLongProperty(request, "company_evaluation_template_id");

            if (templateId == null || templateId <= 0)
            {
                templateId = await _context.Database.SqlQueryRaw<long>(
                    """
                    SELECT id AS "Value"
                    FROM company_evaluation_templates
                    WHERE internship_id = @internshipId
                    ORDER BY created_at DESC
                    LIMIT 1
                    """,
                    new Npgsql.NpgsqlParameter("internshipId", request.internship_id))
                    .FirstOrDefaultAsync();
            }

            if (templateId == null || templateId <= 0)
            {
                templateId = await CreateDefaultCompanyEvaluationTemplateAsync(request);
            }

            var sendingTemplateName = request.sending_template_name ?? "Company Evaluation Request Email";
            var evaluationTemplateName = request.evaluation_template_name ?? "Standard Company Internship Evaluation";
            var publicToken = Guid.NewGuid();
            var tokenExpiresAt = DateTime.UtcNow.AddDays(14);

            var requestId = await ExecuteScalarLongAsync($"""
                INSERT INTO final_evaluation_requests
                    (
                        internship_id,
                        student_user_id,
                        provider_name,
                        provider_email,
                        sending_template_name,
                        evaluation_template_name,
                        company_evaluation_template_id,
                        requested_by_user_id,
                        status,
                        requested_at,
                        created_at,
                        public_token,
                        token_expires_at
                    )
                VALUES
                    (
                        {request.internship_id},
                        {request.student_user_id},
                        {request.provider_name},
                        {normalizedProviderEmail},
                        {sendingTemplateName},
                        {evaluationTemplateName},
                        {templateId.Value},
                        {request.requested_by_user_id},
                        {"Pending"}::approval_status_enum,
                        NOW(),
                        NOW(),
                        {publicToken},
                        {tokenExpiresAt}
                    )
                RETURNING id
                """);

            var registrationLink =
                $"{GetFrontendBaseUrl()}/external/company-evaluation/register/{publicToken}";

            var emailSent = false;
            var emailNote = "Email was not sent.";

            try
            {
                await SendCompanyEvaluationEmailAsync(
                    normalizedProviderEmail,
                    sendingTemplateName,
                    registrationLink);

                emailSent = true;
                emailNote = "Email was sent successfully through Brevo SMTP.";
            }
            catch (Exception ex)
            {
                emailSent = false;
                emailNote = $"Evaluation request was created, but email sending failed: {ex.Message}";
            }

            return Ok(new
            {
                id = requestId,
                internship_id = request.internship_id,
                student_user_id = request.student_user_id,
                provider_name = request.provider_name,
                provider_email = normalizedProviderEmail,
                sending_template_name = sendingTemplateName,
                evaluation_template_name = evaluationTemplateName,
                company_evaluation_template_id = templateId.Value,
                status = "Pending",
                public_token = publicToken,
                registration_link = registrationLink,
                email_sent = emailSent,
                email_queued = emailSent,
                email_note = emailNote,
                message = "Final evaluation request created successfully."
            });
        }

        [HttpGet("company/internship/{internshipId:long}")]
        public async Task<IActionResult> GetCompanyEvaluations(long internshipId)
        {
            var sql = """
                SELECT
                    ce.id,
                    ce.template_id,
                    ce.internship_id,
                    ce.student_user_id,
                    su.full_name AS student_name,
                    ce.provider_name,
                    ce.provider_email,
                    ce.evaluator_name,
                    ce.evaluator_job_title,
                    ce.notes,
                    ce.submitted_at,
                    ce.status::text AS status,
                    ce.total_percentage
                FROM company_evaluations ce
                JOIN users su ON su.id = ce.student_user_id
                WHERE ce.internship_id = @internshipId
                ORDER BY ce.submitted_at DESC
                """;

            var items = await _context.Database.SqlQueryRaw<CompanyEvaluationListItemDto>(
                sql,
                new Npgsql.NpgsqlParameter("internshipId", internshipId))
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("company/register-link")]
        public async Task<IActionResult> RegisterCompanyEvaluationLink(RegisterCompanyEvaluationLinkRequest request)
        {
            if (request.token == Guid.Empty)
                return BadRequest(new { message = "Evaluation token is required." });

            if (string.IsNullOrWhiteSpace(request.email))
                return BadRequest(new { message = "Company email is required." });

            if (string.IsNullOrWhiteSpace(request.evaluator_name))
                return BadRequest(new { message = "Evaluator name is required." });

            if (string.IsNullOrWhiteSpace(request.evaluator_job_title))
                return BadRequest(new { message = "Evaluator job title is required." });

            var normalizedEmail = request.email.Trim().ToLower();

            var sql = """
                SELECT
                    r.id,
                    r.internship_id,
                    r.student_user_id,
                    su.full_name AS student_name,
                    r.provider_name,
                    r.provider_email,
                    r.company_evaluation_template_id AS template_id,
                    r.status::text AS status,
                    r.registered_email,
                    r.registered_at,
                    r.token_expires_at
                FROM final_evaluation_requests r
                JOIN users su ON su.id = r.student_user_id
                WHERE r.public_token = @token
                LIMIT 1
                """;

            var item = await _context.Database.SqlQueryRaw<CompanyEvaluationLinkLookupRow>(
                sql,
                new Npgsql.NpgsqlParameter("token", request.token))
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = "Evaluation link is invalid." });

            if (item.token_expires_at.HasValue && item.token_expires_at.Value < DateTime.UtcNow)
                return BadRequest(new { message = "Evaluation link has expired." });

            if (!string.Equals(item.status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "This company evaluation request has already been completed or is no longer pending." });
            }

            if (item.registered_at.HasValue || !string.IsNullOrWhiteSpace(item.registered_email))
            {
                return BadRequest(new
                {
                    message = "This evaluation link has already been used for registration."
                });
            }

            if (string.IsNullOrWhiteSpace(item.provider_email))
                return BadRequest(new { message = "Provider email is not configured for this request." });

            var providerEmail = item.provider_email.Trim().ToLower();

            if (normalizedEmail != providerEmail)
            {
                return BadRequest(new
                {
                    message = "Registration is allowed only using the same company email assigned to this evaluation request."
                });
            }

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE final_evaluation_requests
                SET registered_email = {normalizedEmail},
                    registered_at = NOW()
                WHERE id = {item.id}
                  AND registered_at IS NULL
                """);

            var criteria = await _context.company_evaluation_template_criteria
                .AsNoTracking()
                .Where(x => x.template_id == item.template_id)
                .OrderBy(x => x.sort_order)
                .Select(x => new
                {
                    id = x.id,
                    criterion_name = x.criterion_name,
                    weight = x.weight,
                    sort_order = x.sort_order
                })
                .ToListAsync();

            if (criteria.Count == 0)
                return BadRequest(new { message = "No evaluation criteria were found for this request." });

            return Ok(new
            {
                request_id = item.id,
                internship_id = item.internship_id,
                student_user_id = item.student_user_id,
                student_name = item.student_name,
                provider_name = item.provider_name,
                provider_email = item.provider_email,
                template_id = item.template_id,
                evaluator_name = request.evaluator_name,
                evaluator_job_title = request.evaluator_job_title,
                criteria
            });
        }

        [HttpPost("company/submit")]
        public async Task<IActionResult> SubmitCompanyEvaluation(SubmitCompanyEvaluationRequest request)
        {
            if (request.scores == null || request.scores.Count == 0)
                return BadRequest(new { message = "At least one score is required." });

            var requestToken = TryGetGuidProperty(request, "token");
            var requestEmail = TryGetStringProperty(request, "email");

            if (requestToken == null || requestToken == Guid.Empty)
                return BadRequest(new { message = "Evaluation token is required." });

            if (string.IsNullOrWhiteSpace(requestEmail))
                return BadRequest(new { message = "Company email is required." });

            if (string.IsNullOrWhiteSpace(request.evaluator_name))
                return BadRequest(new { message = "evaluator_name is required." });

            if (string.IsNullOrWhiteSpace(request.evaluator_job_title))
                return BadRequest(new { message = "evaluator_job_title is required." });

            var normalizedEmail = requestEmail.Trim().ToLower();

            var accessSql = """
                SELECT
                    r.id,
                    r.internship_id,
                    r.student_user_id,
                    r.provider_name,
                    r.provider_email,
                    r.company_evaluation_template_id AS template_id,
                    r.registered_email,
                    r.registered_at,
                    r.status::text AS status,
                    r.token_expires_at
                FROM final_evaluation_requests r
                WHERE r.public_token = @token
                LIMIT 1
                """;

            var access = await _context.Database.SqlQueryRaw<CompanyEvaluationSubmitAccessRow>(
                accessSql,
                new Npgsql.NpgsqlParameter("token", requestToken.Value))
                .FirstOrDefaultAsync();

            if (access == null)
                return BadRequest(new { message = "Invalid evaluation link." });

            if (access.token_expires_at.HasValue && access.token_expires_at.Value < DateTime.UtcNow)
                return BadRequest(new { message = "Evaluation link has expired." });

            if (!string.Equals(access.status, "Pending", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "This company evaluation request has already been completed." });

            if (string.IsNullOrWhiteSpace(access.provider_email))
                return BadRequest(new { message = "Provider email is not configured for this request." });

            if (!string.Equals(access.provider_email.Trim().ToLower(), normalizedEmail, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "This email is not allowed to submit this evaluation." });

            if (!access.registered_at.HasValue ||
                !string.Equals(access.registered_email?.Trim().ToLower(), normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "Please register first using the assigned company email." });
            }

            if (request.internship_id > 0 && request.internship_id != access.internship_id)
                return BadRequest(new { message = "internship_id does not match the evaluation link." });

            if (request.student_user_id > 0 && request.student_user_id != access.student_user_id)
                return BadRequest(new { message = "student_user_id does not match the evaluation link." });

            var templateId = request.template_id > 0
                ? request.template_id
                : access.template_id.GetValueOrDefault();

            if (templateId <= 0)
                return BadRequest(new { message = "company evaluation template_id is required." });

            if (access.template_id.HasValue && templateId != access.template_id.Value)
                return BadRequest(new { message = "template_id does not match the evaluation link." });

            var alreadySubmitted = await _context.Database.SqlQueryRaw<int>(
                """
                SELECT COUNT(*) AS "Value"
                FROM company_evaluations
                WHERE internship_id = @internshipId
                  AND student_user_id = @studentUserId
                  AND template_id = @templateId
                """,
                new Npgsql.NpgsqlParameter("internshipId", access.internship_id),
                new Npgsql.NpgsqlParameter("studentUserId", access.student_user_id),
                new Npgsql.NpgsqlParameter("templateId", templateId))
                .SingleAsync();

            if (alreadySubmitted > 0)
                return BadRequest(new { message = "This company evaluation has already been submitted." });

            await using var tx = await _context.Database.BeginTransactionAsync();

            var totalOutOf = request.scores.Sum(x => x.out_of);
            var totalScore = request.scores.Sum(x => x.score);
            var percentage = totalOutOf <= 0 ? 0 : Math.Round((totalScore / totalOutOf) * 100m, 2);

            var providerName = string.IsNullOrWhiteSpace(request.provider_name)
                ? access.provider_name
                : request.provider_name;

            var evaluationId = await ExecuteScalarLongAsync($"""
                INSERT INTO company_evaluations
                    (
                        template_id,
                        internship_id,
                        student_user_id,
                        provider_name,
                        provider_email,
                        evaluator_name,
                        evaluator_job_title,
                        notes,
                        submitted_at,
                        status,
                        total_percentage
                    )
                VALUES
                    (
                        {templateId},
                        {access.internship_id},
                        {access.student_user_id},
                        {providerName},
                        {normalizedEmail},
                        {request.evaluator_name},
                        {request.evaluator_job_title},
                        {request.notes},
                        NOW(),
                        {"Submitted"}::evaluation_status_enum,
                        {percentage}
                    )
                RETURNING id
                """);

            foreach (var score in request.scores)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO company_evaluation_scores
                        (company_evaluation_id, criterion_id, criterion_name, score, out_of)
                    VALUES
                        ({evaluationId}, {score.criterion_id}, {score.criterion_name}, {score.score}, {score.out_of})
                    """);
            }

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE final_evaluation_requests
                SET status = {"Approved"}::approval_status_enum,
                    completed_at = NOW()
                WHERE id = {access.id}
                """);

            await tx.CommitAsync();

            return Ok(new
            {
                id = evaluationId,
                total_percentage = percentage,
                message = "Company evaluation submitted successfully."
            });
        }

        [HttpGet("academic/internship/{internshipId:long}")]
        public async Task<IActionResult> GetAcademicEvaluations(long internshipId)
        {
            var sql = """
                SELECT
                    ae.id,
                    ae.internship_id,
                    ae.student_user_id,
                    su.full_name AS student_name,
                    ae.evaluator_user_id,
                    ae.evaluator_name,
                    ae.notes,
                    ae.submitted_at,
                    ae.status::text AS status,
                    ae.total_percentage
                FROM academic_evaluations ae
                JOIN users su ON su.id = ae.student_user_id
                WHERE ae.internship_id = @internshipId
                ORDER BY ae.submitted_at DESC
                """;

            var items = await _context.Database.SqlQueryRaw<AcademicEvaluationListItemDto>(
                sql,
                new Npgsql.NpgsqlParameter("internshipId", internshipId))
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("academic/submit")]
        public async Task<IActionResult> SubmitAcademicEvaluation(SubmitAcademicEvaluationRequest request)
        {
            if (request.scores == null || request.scores.Count == 0)
                return BadRequest(new { message = "At least one score is required." });

            if (request.internship_id <= 0)
                return BadRequest(new { message = "internship_id is required." });

            if (request.student_user_id <= 0)
                return BadRequest(new { message = "student_user_id is required." });

            if (request.evaluator_user_id <= 0)
                return BadRequest(new { message = "evaluator_user_id is required." });

            await using var tx = await _context.Database.BeginTransactionAsync();

            var totalOutOf = request.scores.Sum(x => x.out_of);
            var totalScore = request.scores.Sum(x => x.score);
            var percentage = totalOutOf <= 0 ? 0 : Math.Round((totalScore / totalOutOf) * 100m, 2);

            var existingId = await _context.Database
                .SqlQueryRaw<long>(
                    """
                    SELECT id AS "Value"
                    FROM academic_evaluations
                    WHERE internship_id = @internshipId
                    LIMIT 1
                    """,
                    new Npgsql.NpgsqlParameter("internshipId", request.internship_id))
                .FirstOrDefaultAsync();

            long evaluationId;

            if (existingId > 0)
            {
                evaluationId = existingId;

                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE academic_evaluations
                    SET student_user_id = {request.student_user_id},
                        evaluator_user_id = {request.evaluator_user_id},
                        evaluator_name = {request.evaluator_name},
                        notes = {request.notes},
                        submitted_at = NOW(),
                        status = {"Submitted"}::evaluation_status_enum,
                        total_percentage = {percentage}
                    WHERE id = {evaluationId}
                    """);

                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    DELETE FROM academic_evaluation_scores
                    WHERE academic_evaluation_id = {evaluationId}
                    """);
            }
            else
            {
                evaluationId = await ExecuteScalarLongAsync($"""
                    INSERT INTO academic_evaluations
                        (
                            internship_id,
                            student_user_id,
                            evaluator_user_id,
                            evaluator_name,
                            notes,
                            submitted_at,
                            status,
                            total_percentage
                        )
                    VALUES
                        (
                            {request.internship_id},
                            {request.student_user_id},
                            {request.evaluator_user_id},
                            {request.evaluator_name},
                            {request.notes},
                            NOW(),
                            {"Submitted"}::evaluation_status_enum,
                            {percentage}
                        )
                    RETURNING id
                    """);
            }

            foreach (var score in request.scores)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO academic_evaluation_scores
                        (
                            academic_evaluation_id,
                            criterion_name,
                            score,
                            out_of
                        )
                    VALUES
                        (
                            {evaluationId},
                            {score.criterion_name},
                            {score.score},
                            {score.out_of}
                        )
                    """);
            }

            await tx.CommitAsync();

            return Ok(new
            {
                id = evaluationId,
                total_percentage = percentage,
                message = "Academic evaluation submitted successfully."
            });
        }

        [HttpGet("summary/{internshipId:long}")]
        public async Task<IActionResult> GetSummary(long internshipId)
        {
            var sql = """
                SELECT
                    i.id AS internship_id,
                    i.student_user_id,
                    su.full_name AS student_name,
                    i.provider_name,
                    EXISTS (
                        SELECT 1 FROM final_evaluation_requests fr
                        WHERE fr.internship_id = i.id
                    ) AS has_company_request,
                    EXISTS (
                        SELECT 1 FROM company_evaluations ce
                        WHERE ce.internship_id = i.id
                    ) AS has_company_evaluation,
                    EXISTS (
                        SELECT 1 FROM academic_evaluations ae
                        WHERE ae.internship_id = i.id
                    ) AS has_academic_evaluation,
                    (SELECT ce.total_percentage FROM company_evaluations ce WHERE ce.internship_id = i.id ORDER BY ce.submitted_at DESC LIMIT 1) AS company_total_percentage,
                    (SELECT ae.total_percentage FROM academic_evaluations ae WHERE ae.internship_id = i.id ORDER BY ae.submitted_at DESC LIMIT 1) AS academic_total_percentage,
                    (SELECT ce.status::text FROM company_evaluations ce WHERE ce.internship_id = i.id ORDER BY ce.submitted_at DESC LIMIT 1) AS company_status,
                    (SELECT ae.status::text FROM academic_evaluations ae WHERE ae.internship_id = i.id ORDER BY ae.submitted_at DESC LIMIT 1) AS academic_status
                FROM internships i
                JOIN users su ON su.id = i.student_user_id
                WHERE i.id = @internshipId
                LIMIT 1
                """;

            var item = await _context.Database.SqlQueryRaw<FinalEvaluationSummaryDto>(
                sql,
                new Npgsql.NpgsqlParameter("internshipId", internshipId))
                .FirstOrDefaultAsync();

            if (item == null)
                return NotFound(new { message = "Internship not found." });

            return Ok(item);
        }

        private async Task<long> CreateDefaultCompanyEvaluationTemplateAsync(FinalEvaluationRequestDto request)
        {
            var token = Guid.NewGuid().ToString("N");
            var title = string.IsNullOrWhiteSpace(request.evaluation_template_name)
                ? "Default Company Evaluation Template"
                : request.evaluation_template_name;

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO company_evaluation_templates
                    (
                        internship_id,
                        student_user_id,
                        provider_name,
                        provider_email,
                        title,
                        version,
                        status,
                        token,
                        created_by_user_id,
                        created_at,
                        updated_at
                    )
                VALUES
                    (
                        {request.internship_id},
                        {request.student_user_id},
                        {request.provider_name},
                        {request.provider_email},
                        {title},
                        {1},
                        {"Active"}::evaluation_template_status_enum,
                        {Guid.NewGuid().ToString("N")},
                        {request.requested_by_user_id},
                        NOW(),
                        NOW()
                    )
                """);

            var templateId = await _context.Database.SqlQueryRaw<long>(
                """
                SELECT id AS "Value"
                FROM company_evaluation_templates
                WHERE internship_id = @internshipId
                  AND student_user_id = @studentUserId
                ORDER BY created_at DESC
                LIMIT 1
                """,
                new Npgsql.NpgsqlParameter("internshipId", request.internship_id),
                new Npgsql.NpgsqlParameter("studentUserId", request.student_user_id))
                .SingleAsync();

            var defaultCriteria = new[]
            {
                new { Name = "Attendance and Commitment", Weight = 25m, SortOrder = 1 },
                new { Name = "Work Quality and Productivity", Weight = 25m, SortOrder = 2 },
                new { Name = "Communication and Teamwork", Weight = 25m, SortOrder = 3 },
                new { Name = "Learning Progress and Professional Behavior", Weight = 25m, SortOrder = 4 }
            };

            foreach (var criterion in defaultCriteria)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO company_evaluation_template_criteria
                        (
                            template_id,
                            criterion_name,
                            weight,
                            sort_order
                        )
                    VALUES
                        (
                            {templateId},
                            {criterion.Name},
                            {criterion.Weight},
                            {criterion.SortOrder}
                        )
                    """);
            }

            return templateId;
        }

        private async Task SendCompanyEvaluationEmailAsync(
            string providerEmail,
            string subject,
            string registrationLink)
        {
            if (string.IsNullOrWhiteSpace(providerEmail))
                throw new InvalidOperationException("Provider email is required before sending the evaluation request email.");

            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(BrevoFromName, BrevoFromEmail));
            message.ReplyTo.Add(new MailboxAddress(BrevoReplyToName, BrevoReplyToEmail));
            message.To.Add(MailboxAddress.Parse(providerEmail));
            message.Subject = string.IsNullOrWhiteSpace(subject)
                ? "Company Evaluation Request Email"
                : subject;

            var htmlBody = $"""
                <div style="font-family: Arial, sans-serif; line-height: 1.6; color: #1f2937;">
                    <h2 style="margin-bottom: 12px;">Company Evaluation Request</h2>

                    <p>Dear Training Provider,</p>

                    <p>
                        You have been requested to complete the company evaluation form
                        for the assigned internship student.
                    </p>

                    <p>
                        Please register using the same company email assigned to this request:
                        <strong>{providerEmail}</strong>
                    </p>

                    <p style="margin: 24px 0;">
                        <a href="{registrationLink}"
                           style="display: inline-block; padding: 10px 16px; background: #0d6efd; color: #ffffff; text-decoration: none; border-radius: 6px;">
                            Open Evaluation Link
                        </a>
                    </p>

                    <p>If the button does not work, copy and paste this link into your browser:</p>
                    <p><a href="{registrationLink}">{registrationLink}</a></p>

                    <p>This link is intended for one-time evaluation access only.</p>
                </div>
                """;

            var plainTextBody = $"""
                Company Evaluation Request

                Please complete the company evaluation form using this link:
                {registrationLink}

                You must register using this company email:
                {providerEmail}

                This link is intended for one-time evaluation access only.
                """;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = plainTextBody
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var smtpClient = new SmtpClient();

            await smtpClient.ConnectAsync(
                BrevoSmtpHost,
                BrevoSmtpPort,
                SecureSocketOptions.StartTls);

            await smtpClient.AuthenticateAsync(
                BrevoSmtpLogin,
                BrevoSmtpPassword);

            await smtpClient.SendAsync(message);
            await smtpClient.DisconnectAsync(true);
        }

        private string GetFrontendBaseUrl()
        {
            return (_configuration["Frontend:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        }

        private async Task<long> ExecuteScalarLongAsync(FormattableString sql)
        {
            var connection = _context.Database.GetDbConnection();
            var shouldCloseConnection = connection.State != ConnectionState.Open;

            if (shouldCloseConnection)
                await connection.OpenAsync();

            try
            {
                await using var command = connection.CreateCommand();

                var currentTransaction = _context.Database.CurrentTransaction;
                if (currentTransaction != null)
                    command.Transaction = currentTransaction.GetDbTransaction();

                var arguments = sql.GetArguments();
                var parameterNames = new object[arguments.Length];

                for (var i = 0; i < arguments.Length; i++)
                {
                    var parameterName = $"@p{i}";
                    parameterNames[i] = parameterName;

                    var parameter = command.CreateParameter();
                    parameter.ParameterName = parameterName;
                    parameter.Value = arguments[i] ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }

                command.CommandText = string.Format(
                    CultureInfo.InvariantCulture,
                    sql.Format,
                    parameterNames);

                var result = await command.ExecuteScalarAsync();

                if (result == null || result == DBNull.Value)
                    throw new InvalidOperationException("The database command did not return an id.");

                return Convert.ToInt64(result, CultureInfo.InvariantCulture);
            }
            finally
            {
                if (shouldCloseConnection)
                    await connection.CloseAsync();
            }
        }

        private static long? TryGetLongProperty(object source, string propertyName)
        {
            var property = source.GetType().GetProperty(propertyName);
            if (property == null)
                return null;

            var value = property.GetValue(source);

            if (value == null)
                return null;

            if (value is long longValue)
                return longValue;

            if (value is int intValue)
                return intValue;

            if (long.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed))
                return parsed;

            return null;
        }

        private static Guid? TryGetGuidProperty(object source, string propertyName)
        {
            var property = source.GetType().GetProperty(propertyName);
            if (property == null)
                return null;

            var value = property.GetValue(source);

            if (value == null)
                return null;

            if (value is Guid guidValue)
                return guidValue;

            if (Guid.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var parsed))
                return parsed;

            return null;
        }

        private static string? TryGetStringProperty(object source, string propertyName)
        {
            var property = source.GetType().GetProperty(propertyName);
            if (property == null)
                return null;

            return property.GetValue(source)?.ToString();
        }

        private sealed class FinalEvaluationRequestRow
        {
            public long id { get; set; }
            public long internship_id { get; set; }
            public long student_user_id { get; set; }
            public string student_name { get; set; } = string.Empty;
            public string? provider_name { get; set; }
            public string? provider_email { get; set; }
            public string? sending_template_name { get; set; }
            public string? evaluation_template_name { get; set; }
            public long? company_evaluation_template_id { get; set; }
            public Guid? public_token { get; set; }
            public long requested_by_user_id { get; set; }
            public string requested_by_name { get; set; } = string.Empty;
            public string status { get; set; } = string.Empty;
            public DateTime? requested_at { get; set; }
            public DateTime? completed_at { get; set; }
        }

        private sealed class CompanyEvaluationLinkLookupRow
        {
            public long id { get; set; }
            public long internship_id { get; set; }
            public long student_user_id { get; set; }
            public string student_name { get; set; } = string.Empty;
            public string? provider_name { get; set; }
            public string? provider_email { get; set; }
            public long? template_id { get; set; }
            public string status { get; set; } = string.Empty;
            public string? registered_email { get; set; }
            public DateTime? registered_at { get; set; }
            public DateTime? token_expires_at { get; set; }
        }

        private sealed class CompanyEvaluationSubmitAccessRow
        {
            public long id { get; set; }
            public long internship_id { get; set; }
            public long student_user_id { get; set; }
            public string? provider_name { get; set; }
            public string? provider_email { get; set; }
            public long? template_id { get; set; }
            public string? registered_email { get; set; }
            public DateTime? registered_at { get; set; }
            public string status { get; set; } = string.Empty;
            public DateTime? token_expires_at { get; set; }
        }
    }
}