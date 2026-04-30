using IMS.Api.Data;
using IMS.Api.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("user/{userId:long}")]
        public async Task<IActionResult> GetUserNotifications(long userId)
        {
            var sql = """
                SELECT
                    id,
                    user_id,
                    title,
                    message,
                    recipient_email,
                    type::text AS type,
                    channel::text AS channel,
                    status::text AS status,
                    related_entity_type,
                    related_entity_id,
                    created_at,
                    sent_at,
                    read_at
                FROM notifications
                WHERE user_id = @userId
                ORDER BY created_at DESC;
                """;

            var items = await _context.Database.SqlQueryRaw<NotificationListItemDto>(
                sql,
                new Npgsql.NpgsqlParameter("userId", userId))
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("{id:long}/read")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var exists = await _context.notifications.AnyAsync(x => x.id == id);
            if (!exists)
                return NotFound(new { message = "Notification not found." });

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE notifications
                SET status = {"Read"}::notification_status_enum,
                    read_at = NOW()
                WHERE id = {id};
                """);

            return Ok(new { message = "Notification marked as read." });
        }
    }
}