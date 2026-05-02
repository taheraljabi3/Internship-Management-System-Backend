using System.Data;
using System.Globalization;
using System.Security.Claims;
using IMS.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Administrator")]
    public class AdministrationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdministrationController(AppDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // 4. Notifications
        // =========================================================

[HttpGet("notifications")]
public async Task<IActionResult> GetNotifications(
    [FromQuery] string? q = null,
    [FromQuery] string? type = null,
    [FromQuery] string? status = null)
{
    var searchValue = string.IsNullOrWhiteSpace(q) ? "" : $"%{q.Trim()}%";
    var typeValue = string.IsNullOrWhiteSpace(type) || type.Equals("All", StringComparison.OrdinalIgnoreCase)
        ? ""
        : type.Trim();

    var statusValue = string.IsNullOrWhiteSpace(status) || status.Equals("All", StringComparison.OrdinalIgnoreCase)
        ? ""
        : status.Trim();

    var rows = await _context.Database.SqlQueryRaw<AdminNotificationDto>(
        """
        SELECT
            n.id,
            n.title,
            n.message,
            n.recipient_user_id,
            u.full_name AS recipient_name,
            n.recipient_email,
            n.type,
            n.status,
            n.created_at,
            n.read_at
        FROM admin_notifications n
        LEFT JOIN users u ON u.id = n.recipient_user_id
        WHERE (
            @q = ''
            OR n.title ILIKE @q
            OR COALESCE(n.message, '') ILIKE @q
            OR COALESCE(n.recipient_email, '') ILIKE @q
        )
        AND (
            @type = ''
            OR n.type = @type
        )
        AND (
            @status = ''
            OR n.status = @status
        )
        ORDER BY n.created_at DESC, n.id DESC
        """,
        new NpgsqlParameter("q", NpgsqlTypes.NpgsqlDbType.Text) { Value = searchValue },
        new NpgsqlParameter("type", NpgsqlTypes.NpgsqlDbType.Text) { Value = typeValue },
        new NpgsqlParameter("status", NpgsqlTypes.NpgsqlDbType.Text) { Value = statusValue })
        .ToListAsync();

    return Ok(rows);
}

        [HttpPost("notifications")]
        public async Task<IActionResult> CreateNotification(CreateAdminNotificationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.title))
                return BadRequest(new { message = "title is required." });

            var notificationId = await ExecuteScalarLongAsync($"""
                INSERT INTO admin_notifications
                    (
                        title,
                        message,
                        recipient_user_id,
                        recipient_email,
                        type,
                        status,
                        created_at
                    )
                VALUES
                    (
                        {request.title.Trim()},
                        {request.message},
                        {request.recipient_user_id},
                        {request.recipient_email},
                        {NormalizeStatusValue(request.type, "System")},
                        {NormalizeStatusValue(request.status, "Pending")},
                        NOW()
                    )
                RETURNING id
                """);

            await WriteAuditLogAsync(
                "Create",
                "admin_notifications",
                notificationId.ToString(CultureInfo.InvariantCulture),
                $"Created notification: {request.title}");

            return Ok(new
            {
                id = notificationId,
                message = "Notification created successfully."
            });
        }

        [HttpPut("notifications/{id:long}")]
        public async Task<IActionResult> UpdateNotification(long id, UpdateAdminNotificationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.title))
                return BadRequest(new { message = "title is required." });

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE admin_notifications
                SET title = {request.title.Trim()},
                    message = {request.message},
                    recipient_user_id = {request.recipient_user_id},
                    recipient_email = {request.recipient_email},
                    type = {NormalizeStatusValue(request.type, "System")},
                    status = {NormalizeStatusValue(request.status, "Pending")},
                    read_at = {request.read_at}
                WHERE id = {id}
                """);

            if (affected == 0)
                return NotFound(new { message = "Notification not found." });

            await WriteAuditLogAsync(
                "Update",
                "admin_notifications",
                id.ToString(CultureInfo.InvariantCulture),
                $"Updated notification: {request.title}");

            return Ok(new { message = "Notification updated successfully." });
        }

        [HttpDelete("notifications/{id:long}")]
        public async Task<IActionResult> DeleteNotification(long id)
        {
            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM admin_notifications
                WHERE id = {id}
                """);

            if (affected == 0)
                return NotFound(new { message = "Notification not found." });

            await WriteAuditLogAsync(
                "Delete",
                "admin_notifications",
                id.ToString(CultureInfo.InvariantCulture),
                "Deleted notification.");

            return Ok(new { message = "Notification deleted successfully." });
        }

        // =========================================================
        // 5. Audit Logs
        // =========================================================
        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] string? q = null,
            [FromQuery] string? entity = null,
            [FromQuery] int limit = 200)
        {
            limit = Math.Clamp(limit, 1, 1000);

            var rows = await _context.Database.SqlQueryRaw<AdminAuditLogDto>(
                """
                SELECT
                    id,
                    actor_user_id,
                    actor_name,
                    action,
                    entity_name,
                    entity_id,
                    details,
                    ip_address,
                    created_at
                FROM admin_audit_logs
                WHERE (@q IS NULL OR (COALESCE(actor_name, '') ILIKE @q OR action ILIKE @q OR COALESCE(entity_name, '') ILIKE @q OR COALESCE(details, '') ILIKE @q))
                  AND (@entity IS NULL OR entity_name = @entity)
                ORDER BY created_at DESC, id DESC
                LIMIT @limit
                """,
                new NpgsqlParameter("q", NormalizeSearch(q)),
                new NpgsqlParameter("entity", NormalizeFilter(entity)),
                new NpgsqlParameter("limit", limit))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpPost("audit-logs")]
        public async Task<IActionResult> CreateAuditLog(CreateAdminAuditLogRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.action))
                return BadRequest(new { message = "action is required." });

            var actorUserId = request.actor_user_id ?? GetCurrentUserId();
            var actorName = request.actor_name ?? GetCurrentUserName();
            var ipAddress = request.ip_address ?? HttpContext.Connection.RemoteIpAddress?.ToString();

            var auditLogId = await ExecuteScalarLongAsync($"""
                INSERT INTO admin_audit_logs
                    (
                        actor_user_id,
                        actor_name,
                        action,
                        entity_name,
                        entity_id,
                        details,
                        ip_address,
                        created_at
                    )
                VALUES
                    (
                        {actorUserId},
                        {actorName},
                        {request.action.Trim()},
                        {request.entity_name},
                        {request.entity_id},
                        {request.details},
                        {ipAddress},
                        NOW()
                    )
                RETURNING id
                """);

            return Ok(new
            {
                id = auditLogId,
                message = "Audit log created successfully."
            });
        }

        // =========================================================
        // 6. System Settings
        // =========================================================
        [HttpGet("system-settings")]
        public async Task<IActionResult> GetSystemSettings(
            [FromQuery] string? q = null,
            [FromQuery] string? category = null)
        {
            var rows = await _context.Database.SqlQueryRaw<AdminSystemSettingDto>(
                """
                SELECT
                    id,
                    setting_key,
                    CASE WHEN is_sensitive THEN '********' ELSE setting_value END AS setting_value,
                    category,
                    description,
                    is_sensitive,
                    updated_at
                FROM admin_system_settings
                WHERE (@q IS NULL OR (setting_key ILIKE @q OR COALESCE(setting_value, '') ILIKE @q OR COALESCE(description, '') ILIKE @q))
                  AND (@category IS NULL OR category = @category)
                ORDER BY category NULLS LAST, setting_key
                """,
                new NpgsqlParameter("q", NormalizeSearch(q)),
                new NpgsqlParameter("category", NormalizeFilter(category)))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpPost("system-settings")]
        public async Task<IActionResult> CreateSystemSetting(CreateAdminSystemSettingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.setting_key))
                return BadRequest(new { message = "setting_key is required." });

            var settingId = await ExecuteScalarLongAsync($"""
                INSERT INTO admin_system_settings
                    (
                        setting_key,
                        setting_value,
                        category,
                        description,
                        is_sensitive,
                        updated_at
                    )
                VALUES
                    (
                        {request.setting_key.Trim()},
                        {request.setting_value},
                        {request.category},
                        {request.description},
                        {request.is_sensitive},
                        NOW()
                    )
                RETURNING id
                """);

            await WriteAuditLogAsync(
                "Create",
                "admin_system_settings",
                settingId.ToString(CultureInfo.InvariantCulture),
                $"Created system setting: {request.setting_key}");

            return Ok(new
            {
                id = settingId,
                message = "System setting created successfully."
            });
        }

        [HttpPut("system-settings/{id:long}")]
        public async Task<IActionResult> UpdateSystemSetting(long id, UpdateAdminSystemSettingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.setting_key))
                return BadRequest(new { message = "setting_key is required." });

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE admin_system_settings
                SET setting_key = {request.setting_key.Trim()},
                    setting_value = {request.setting_value},
                    category = {request.category},
                    description = {request.description},
                    is_sensitive = {request.is_sensitive},
                    updated_at = NOW()
                WHERE id = {id}
                """);

            if (affected == 0)
                return NotFound(new { message = "System setting not found." });

            await WriteAuditLogAsync(
                "Update",
                "admin_system_settings",
                id.ToString(CultureInfo.InvariantCulture),
                $"Updated system setting: {request.setting_key}");

            return Ok(new { message = "System setting updated successfully." });
        }

        [HttpDelete("system-settings/{id:long}")]
        public async Task<IActionResult> DeleteSystemSetting(long id)
        {
            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM admin_system_settings
                WHERE id = {id}
                """);

            if (affected == 0)
                return NotFound(new { message = "System setting not found." });

            await WriteAuditLogAsync(
                "Delete",
                "admin_system_settings",
                id.ToString(CultureInfo.InvariantCulture),
                "Deleted system setting.");

            return Ok(new { message = "System setting deleted successfully." });
        }

        // =========================================================
        // 7. Backup Jobs
        // =========================================================
        [HttpGet("backup-jobs")]
        public async Task<IActionResult> GetBackupJobs(
            [FromQuery] string? q = null,
            [FromQuery] string? status = null)
        {
            var rows = await _context.Database.SqlQueryRaw<AdminBackupJobDto>(
                """
                SELECT
                    id,
                    job_name,
                    job_type,
                    schedule,
                    status,
                    last_run_at,
                    last_result,
                    created_at,
                    updated_at
                FROM admin_backup_jobs
                WHERE (@q IS NULL OR (job_name ILIKE @q OR COALESCE(job_type, '') ILIKE @q OR COALESCE(schedule, '') ILIKE @q))
                  AND (@status IS NULL OR status = @status)
                ORDER BY created_at DESC, id DESC
                """,
                new NpgsqlParameter("q", NormalizeSearch(q)),
                new NpgsqlParameter("status", NormalizeFilter(status)))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpPost("backup-jobs")]
        public async Task<IActionResult> CreateBackupJob(CreateAdminBackupJobRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.job_name))
                return BadRequest(new { message = "job_name is required." });

            var jobId = await ExecuteScalarLongAsync($"""
                INSERT INTO admin_backup_jobs
                    (
                        job_name,
                        job_type,
                        schedule,
                        status,
                        last_result,
                        created_at,
                        updated_at
                    )
                VALUES
                    (
                        {request.job_name.Trim()},
                        {NormalizeStatusValue(request.job_type, "Database")},
                        {request.schedule},
                        {NormalizeStatusValue(request.status, "Pending")},
                        {request.last_result},
                        NOW(),
                        NOW()
                    )
                RETURNING id
                """);

            await WriteAuditLogAsync(
                "Create",
                "admin_backup_jobs",
                jobId.ToString(CultureInfo.InvariantCulture),
                $"Created backup job: {request.job_name}");

            return Ok(new
            {
                id = jobId,
                message = "Backup job created successfully."
            });
        }

        [HttpPut("backup-jobs/{id:long}")]
        public async Task<IActionResult> UpdateBackupJob(long id, UpdateAdminBackupJobRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.job_name))
                return BadRequest(new { message = "job_name is required." });

            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE admin_backup_jobs
                SET job_name = {request.job_name.Trim()},
                    job_type = {NormalizeStatusValue(request.job_type, "Database")},
                    schedule = {request.schedule},
                    status = {NormalizeStatusValue(request.status, "Pending")},
                    last_result = {request.last_result},
                    updated_at = NOW()
                WHERE id = {id}
                """);

            if (affected == 0)
                return NotFound(new { message = "Backup job not found." });

            await WriteAuditLogAsync(
                "Update",
                "admin_backup_jobs",
                id.ToString(CultureInfo.InvariantCulture),
                $"Updated backup job: {request.job_name}");

            return Ok(new { message = "Backup job updated successfully." });
        }

        [HttpPost("backup-jobs/{id:long}/run")]
        public async Task<IActionResult> RunBackupJob(long id)
        {
            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE admin_backup_jobs
                SET status = {"Completed"},
                    last_run_at = NOW(),
                    last_result = {"Backup job was marked as completed manually."},
                    updated_at = NOW()
                WHERE id = {id}
                """);

            if (affected == 0)
                return NotFound(new { message = "Backup job not found." });

            await WriteAuditLogAsync(
                "Run",
                "admin_backup_jobs",
                id.ToString(CultureInfo.InvariantCulture),
                "Manual backup job run was recorded.");

            return Ok(new { message = "Backup job run was recorded successfully." });
        }

        [HttpDelete("backup-jobs/{id:long}")]
        public async Task<IActionResult> DeleteBackupJob(long id)
        {
            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM admin_backup_jobs
                WHERE id = {id}
                """);

            if (affected == 0)
                return NotFound(new { message = "Backup job not found." });

            await WriteAuditLogAsync(
                "Delete",
                "admin_backup_jobs",
                id.ToString(CultureInfo.InvariantCulture),
                "Deleted backup job.");

            return Ok(new { message = "Backup job deleted successfully." });
        }

        // =========================================================
        // 8. Archived Records
        // =========================================================
        [HttpGet("archived-records")]
        public async Task<IActionResult> GetArchivedRecords(
            [FromQuery] string? q = null,
            [FromQuery] string? entity = null,
            [FromQuery] int limit = 200)
        {
            limit = Math.Clamp(limit, 1, 1000);

            var rows = await _context.Database.SqlQueryRaw<AdminArchivedRecordDto>(
                """
                SELECT
                    ar.id,
                    ar.entity_name,
                    ar.entity_id,
                    ar.record_reference,
                    ar.archived_by_user_id,
                    u.full_name AS archived_by_name,
                    ar.archived_at,
                    ar.reason,
                    ar.snapshot_json::text AS snapshot_json
                FROM admin_archived_records ar
                LEFT JOIN users u ON u.id = ar.archived_by_user_id
                WHERE (@q IS NULL OR (ar.entity_name ILIKE @q OR COALESCE(ar.entity_id, '') ILIKE @q OR COALESCE(ar.record_reference, '') ILIKE @q OR COALESCE(ar.reason, '') ILIKE @q))
                  AND (@entity IS NULL OR ar.entity_name = @entity)
                ORDER BY ar.archived_at DESC, ar.id DESC
                LIMIT @limit
                """,
                new NpgsqlParameter("q", NormalizeSearch(q)),
                new NpgsqlParameter("entity", NormalizeFilter(entity)),
                new NpgsqlParameter("limit", limit))
                .ToListAsync();

            return Ok(rows);
        }

        [HttpPost("archived-records")]
        public async Task<IActionResult> CreateArchivedRecord(CreateAdminArchivedRecordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.entity_name))
                return BadRequest(new { message = "entity_name is required." });

            var snapshotJson = string.IsNullOrWhiteSpace(request.snapshot_json)
                ? null
                : request.snapshot_json;

            var recordId = await ExecuteScalarLongAsync($"""
                INSERT INTO admin_archived_records
                    (
                        entity_name,
                        entity_id,
                        record_reference,
                        archived_by_user_id,
                        archived_at,
                        reason,
                        snapshot_json
                    )
                VALUES
                    (
                        {request.entity_name.Trim()},
                        {request.entity_id},
                        {request.record_reference},
                        {request.archived_by_user_id ?? GetCurrentUserId()},
                        NOW(),
                        {request.reason},
                        CAST({snapshotJson} AS jsonb)
                    )
                RETURNING id
                """);

            await WriteAuditLogAsync(
                "Create",
                "admin_archived_records",
                recordId.ToString(CultureInfo.InvariantCulture),
                $"Archived record for entity: {request.entity_name}");

            return Ok(new
            {
                id = recordId,
                message = "Archived record created successfully."
            });
        }

        [HttpDelete("archived-records/{id:long}")]
        public async Task<IActionResult> DeleteArchivedRecord(long id)
        {
            var affected = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM admin_archived_records
                WHERE id = {id}
                """);

            if (affected == 0)
                return NotFound(new { message = "Archived record not found." });

            await WriteAuditLogAsync(
                "Delete",
                "admin_archived_records",
                id.ToString(CultureInfo.InvariantCulture),
                "Deleted archived record.");

            return Ok(new { message = "Archived record deleted successfully." });
        }

        // =========================================================
        // Helpers
        // =========================================================
        private async Task WriteAuditLogAsync(string action, string entityName, string? entityId, string? details)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO admin_audit_logs
                    (
                        actor_user_id,
                        actor_name,
                        action,
                        entity_name,
                        entity_id,
                        details,
                        ip_address,
                        created_at
                    )
                VALUES
                    (
                        {GetCurrentUserId()},
                        {GetCurrentUserName()},
                        {action},
                        {entityName},
                        {entityId},
                        {details},
                        {ipAddress},
                        NOW()
                    )
                """);
        }

        private static object NormalizeSearch(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? DBNull.Value
                : $"%{value.Trim()}%";
        }

        private static object NormalizeFilter(string? value)
        {
            return string.IsNullOrWhiteSpace(value) || string.Equals(value, "All", StringComparison.OrdinalIgnoreCase)
                ? DBNull.Value
                : value.Trim();
        }

        private static string NormalizeStatusValue(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value)
                ? fallback
                : value.Trim();
        }

        private long? GetCurrentUserId()
        {
            var raw =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub");

            return long.TryParse(raw, out var id) ? id : null;
        }

        private string? GetCurrentUserName()
        {
            return
                User.FindFirstValue("full_name") ??
                User.FindFirstValue(ClaimTypes.Name) ??
                User.FindFirstValue(ClaimTypes.Email) ??
                User.Identity?.Name;
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
    }

    public class AdminNotificationDto
    {
        public long id { get; set; }
        public string title { get; set; } = string.Empty;
        public string? message { get; set; }
        public long? recipient_user_id { get; set; }
        public string? recipient_name { get; set; }
        public string? recipient_email { get; set; }
        public string type { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public DateTime created_at { get; set; }
        public DateTime? read_at { get; set; }
    }

    public class CreateAdminNotificationRequest
    {
        public string title { get; set; } = string.Empty;
        public string? message { get; set; }
        public long? recipient_user_id { get; set; }
        public string? recipient_email { get; set; }
        public string? type { get; set; }
        public string? status { get; set; }
    }

    public class UpdateAdminNotificationRequest : CreateAdminNotificationRequest
    {
        public DateTime? read_at { get; set; }
    }

    public class AdminAuditLogDto
    {
        public long id { get; set; }
        public long? actor_user_id { get; set; }
        public string? actor_name { get; set; }
        public string action { get; set; } = string.Empty;
        public string? entity_name { get; set; }
        public string? entity_id { get; set; }
        public string? details { get; set; }
        public string? ip_address { get; set; }
        public DateTime created_at { get; set; }
    }

    public class CreateAdminAuditLogRequest
    {
        public long? actor_user_id { get; set; }
        public string? actor_name { get; set; }
        public string action { get; set; } = string.Empty;
        public string? entity_name { get; set; }
        public string? entity_id { get; set; }
        public string? details { get; set; }
        public string? ip_address { get; set; }
    }

    public class AdminSystemSettingDto
    {
        public long id { get; set; }
        public string setting_key { get; set; } = string.Empty;
        public string? setting_value { get; set; }
        public string? category { get; set; }
        public string? description { get; set; }
        public bool is_sensitive { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class CreateAdminSystemSettingRequest
    {
        public string setting_key { get; set; } = string.Empty;
        public string? setting_value { get; set; }
        public string? category { get; set; }
        public string? description { get; set; }
        public bool is_sensitive { get; set; }
    }

    public class UpdateAdminSystemSettingRequest : CreateAdminSystemSettingRequest
    {
    }

    public class AdminBackupJobDto
    {
        public long id { get; set; }
        public string job_name { get; set; } = string.Empty;
        public string job_type { get; set; } = string.Empty;
        public string? schedule { get; set; }
        public string status { get; set; } = string.Empty;
        public DateTime? last_run_at { get; set; }
        public string? last_result { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    public class CreateAdminBackupJobRequest
    {
        public string job_name { get; set; } = string.Empty;
        public string? job_type { get; set; }
        public string? schedule { get; set; }
        public string? status { get; set; }
        public string? last_result { get; set; }
    }

    public class UpdateAdminBackupJobRequest : CreateAdminBackupJobRequest
    {
    }

    public class AdminArchivedRecordDto
    {
        public long id { get; set; }
        public string entity_name { get; set; } = string.Empty;
        public string? entity_id { get; set; }
        public string? record_reference { get; set; }
        public long? archived_by_user_id { get; set; }
        public string? archived_by_name { get; set; }
        public DateTime archived_at { get; set; }
        public string? reason { get; set; }
        public string? snapshot_json { get; set; }
    }

    public class CreateAdminArchivedRecordRequest
    {
        public string entity_name { get; set; } = string.Empty;
        public string? entity_id { get; set; }
        public string? record_reference { get; set; }
        public long? archived_by_user_id { get; set; }
        public string? reason { get; set; }
        public string? snapshot_json { get; set; }
    }
}