using IMS.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace IMS.Api.Helpers
{
    public static class WorkflowWriteHelper
    {
        public static Task QueueInAppAsync(
            AppDbContext context,
            long? userId,
            string title,
            string? message,
            string relatedEntityType,
            long? relatedEntityId)
        {
            return context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO notifications
                    (user_id, title, message, recipient_email, type, channel, status, related_entity_type, related_entity_id, created_at)
                VALUES
                    ({userId}, {title}, {message}, NULL, {"System"}::notification_type_enum, {"InApp"}::notification_channel_enum, {"Pending"}::notification_status_enum, {relatedEntityType}, {relatedEntityId}, NOW());
                """);
        }

        public static Task QueueEmailAsync(
            AppDbContext context,
            string toEmail,
            string subject,
            string body,
            string relatedEntityType,
            long? relatedEntityId)
        {
            return context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO email_outbox
                    (
                        to_email,
                        subject,
                        body,
                        template_id,
                        related_entity_type,
                        related_entity_id,
                        queued_at
                    )
                VALUES
                    (
                        {toEmail},
                        {subject},
                        {body},
                        NULL,
                        {relatedEntityType},
                        {relatedEntityId},
                        NOW()
                    )
                """);
        }
        public static Task LogApprovalActionAsync(
            AppDbContext context,
            string requestType,
            long requestId,
            long? studentUserId,
            long actorUserId,
            string actionStatus,
            string? comment)
        {
            return context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO approval_actions
                    (request_type, request_id, student_user_id, actor_user_id, action_status, comment, acted_at)
                VALUES
                    ({requestType}::request_type_enum, {requestId}, {studentUserId}, {actorUserId}, {actionStatus}::approval_status_enum, {comment}, NOW());
                """);
        }

        public static Task LogDelegationAsync(
            AppDbContext context,
            string requestType,
            long requestId,
            long? studentUserId,
            long? fromOwnerUserId,
            long toOwnerUserId,
            string? fromOwnerRole,
            string toOwnerRole,
            string reason,
            long changedByUserId)
        {
            return context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO approval_delegation_logs
                    (request_type, request_id, student_user_id, from_owner_user_id, to_owner_user_id, from_owner_role, to_owner_role, reason, changed_by_user_id, changed_at)
                VALUES
                    ({requestType}::request_type_enum, {requestId}, {studentUserId}, {fromOwnerUserId}, {toOwnerUserId}, 
                     {(fromOwnerRole == null ? null : fromOwnerRole)}::approver_type_enum, {toOwnerRole}::approver_type_enum, {reason}, {changedByUserId}, NOW());
                """);
        }

        public static Task LogAuditAsync(
            AppDbContext context,
            long? actorUserId,
            string action,
            string entityName,
            string? entityId,
            string? metadataJson = null)
        {
            return context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO audit_logs
                    (actor_user_id, action, entity_name, entity_id, metadata, created_at)
                VALUES
                    ({actorUserId}, {action}, {entityName}, {entityId}, {metadataJson}::jsonb, NOW());
                """);
        }
    }
}