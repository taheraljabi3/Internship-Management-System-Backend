using IMS.Api.Data;
using IMS.Api.DTOs;
using IMS.Api.Helpers;
using IMS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PasswordService _passwordService;
        private readonly JwtTokenService _jwtTokenService;

        public AuthController(
            AppDbContext context,
            PasswordService passwordService,
            JwtTokenService jwtTokenService)
        {
            _context = context;
            _passwordService = passwordService;
            _jwtTokenService = jwtTokenService;
        }

        [AllowAnonymous]
        [HttpPost("bootstrap-admin")]
        public async Task<IActionResult> BootstrapAdmin(BootstrapAdminRequest request)
        {
            var adminRole = await _context.roles
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.code == "Administrator");

            if (adminRole == null)
                return BadRequest(new { message = "Administrator role not found. Run the schema seed first." });

            var adminExists = await _context.user_roles
                .AsNoTracking()
                .AnyAsync(x => x.role_id == adminRole.id);

            if (adminExists)
                return BadRequest(new { message = "Administrator already exists." });

            var emailExists = await _context.users
                .AsNoTracking()
                .AnyAsync(x => x.email.ToLower() == request.email.ToLower());

            if (emailExists)
                return BadRequest(new { message = "Email already exists." });

            var passwordHash = _passwordService.Hash(request.password);

            await using var tx = await _context.Database.BeginTransactionAsync();

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO users
                    (full_name, email, username, phone, password_hash, status, is_email_confirmed, created_at, updated_at)
                VALUES
                    ({request.full_name}, {request.email}, {request.username}, {request.phone}, {passwordHash}, {"Active"}::user_status_enum, TRUE, NOW(), NOW())
                """);

            var userId = await _context.users
                .AsNoTracking()
                .Where(x => x.email.ToLower() == request.email.ToLower())
                .Select(x => x.id)
                .SingleAsync();

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO user_roles
                    (user_id, role_id, created_at)
                VALUES
                    ({userId}, {adminRole.id}, NOW())
                """);

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO administrator_profiles
                    (user_id, employee_no, department, created_at, updated_at)
                VALUES
                    ({userId}, {request.employee_no}, {request.department}, NOW(), NOW())
                """);

            await tx.CommitAsync();

            return Ok(new { id = userId, message = "Bootstrap administrator created successfully." });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var login = request.login?.Trim();
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(request.password))
                return BadRequest(new { message = "Login and password are required." });

            var sql = """
                SELECT
                    u.id,
                    u.full_name,
                    u.email,
                    u.username,
                    u.phone,
                    u.password_hash,
                    u.status::text AS status,
                    r.code AS role
                FROM users u
                LEFT JOIN user_roles ur ON ur.user_id = u.id
                LEFT JOIN roles r ON r.id = ur.role_id
                WHERE LOWER(u.email) = LOWER(@login)
                   OR LOWER(COALESCE(u.username, '')) = LOWER(@login)
                ORDER BY ur.id
                LIMIT 1
                """;

            var rows = await _context.Database.SqlQueryRaw<LoginUserRow>(
                sql,
                new Npgsql.NpgsqlParameter("login", login))
                .ToListAsync();

            var userRow = rows.FirstOrDefault();

            if (userRow == null)
                return Unauthorized(new { message = "Invalid credentials." });

            if (userRow.status != "Active")
                return Unauthorized(new { message = "User is not active." });

            if (string.IsNullOrWhiteSpace(userRow.password_hash))
                return Unauthorized(new { message = "Password is not set for this user." });

            var validPassword = _passwordService.Verify(request.password, userRow.password_hash);
            if (!validPassword)
                return Unauthorized(new { message = "Invalid credentials." });

            var currentUser = new CurrentUserDto
            {
                id = userRow.id,
                full_name = userRow.full_name,
                email = userRow.email,
                username = userRow.username,
                phone = userRow.phone,
                role = userRow.role ?? string.Empty,
                status = userRow.status
            };

            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenHash = _passwordService.Hash(refreshToken);
            var refreshExpiryUtc = _jwtTokenService.GetRefreshTokenExpiryUtc();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers.UserAgent.ToString();

            var sessionRows = await _context.Database.SqlQuery<long>($"""
                INSERT INTO user_sessions
                    (user_id, refresh_token_hash, ip_address, user_agent, expires_at, created_at)
                VALUES
                    ({currentUser.id}, {refreshTokenHash}, {ipAddress}, {userAgent}, {refreshExpiryUtc}, NOW())
                RETURNING id AS "Value"
                """).ToListAsync();

            var sessionId = sessionRows.First();

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE users
                SET last_login_at = NOW(),
                    updated_at = NOW()
                WHERE id = {currentUser.id}
                """);

            var accessToken = _jwtTokenService.GenerateAccessToken(currentUser, sessionId);

            return Ok(new AuthResponse
            {
                access_token = accessToken,
                refresh_token = refreshToken,
                session_id = sessionId,
                expires_at_utc = _jwtTokenService.GetAccessTokenExpiryUtc(),
                user = currentUser
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.GetUserId();

            var sql = """
                SELECT
                    u.id,
                    u.full_name,
                    u.email,
                    u.username,
                    u.phone,
                    u.status::text AS status,
                    r.code AS role
                FROM users u
                LEFT JOIN user_roles ur ON ur.user_id = u.id
                LEFT JOIN roles r ON r.id = ur.role_id
                WHERE u.id = @userId
                ORDER BY ur.id
                LIMIT 1
                """;

            var rows = await _context.Database.SqlQueryRaw<MeUserRow>(
                sql,
                new Npgsql.NpgsqlParameter("userId", userId))
                .ToListAsync();

            var item = rows.FirstOrDefault();

            if (item == null)
                return NotFound(new { message = "User not found." });

            return Ok(new CurrentUserDto
            {
                id = item.id,
                full_name = item.full_name,
                email = item.email,
                username = item.username,
                phone = item.phone,
                role = item.role ?? string.Empty,
                status = item.status
            });
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequest request)
        {
            var session = await _context.user_sessions
                .AsNoTracking()
                .Where(x => x.id == request.session_id && x.revoked_at == null && x.expires_at > DateTime.UtcNow)
                .Select(x => new RefreshSessionRow
                {
                    id = x.id,
                    user_id = x.user_id,
                    refresh_token_hash = x.refresh_token_hash!,
                    expires_at = x.expires_at
                })
                .FirstOrDefaultAsync();

            if (session == null)
                return Unauthorized(new { message = "Invalid refresh session." });

            var validRefresh = _passwordService.Verify(request.refresh_token, session.refresh_token_hash);
            if (!validRefresh)
                return Unauthorized(new { message = "Invalid refresh token." });

            var sql = """
                SELECT
                    u.id,
                    u.full_name,
                    u.email,
                    u.username,
                    u.phone,
                    u.status::text AS status,
                    r.code AS role
                FROM users u
                LEFT JOIN user_roles ur ON ur.user_id = u.id
                LEFT JOIN roles r ON r.id = ur.role_id
                WHERE u.id = @userId
                ORDER BY ur.id
                LIMIT 1
                """;

            var rows = await _context.Database.SqlQueryRaw<MeUserRow>(
                sql,
                new Npgsql.NpgsqlParameter("userId", session.user_id))
                .ToListAsync();

            var userRow = rows.FirstOrDefault();

            if (userRow == null || userRow.status != "Active")
                return Unauthorized(new { message = "User is not active." });

            var currentUser = new CurrentUserDto
            {
                id = userRow.id,
                full_name = userRow.full_name,
                email = userRow.email,
                username = userRow.username,
                phone = userRow.phone,
                role = userRow.role ?? string.Empty,
                status = userRow.status
            };

            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
            var newRefreshHash = _passwordService.Hash(newRefreshToken);
            var newRefreshExpiryUtc = _jwtTokenService.GetRefreshTokenExpiryUtc();

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE user_sessions
                SET refresh_token_hash = {newRefreshHash},
                    expires_at = {newRefreshExpiryUtc}
                WHERE id = {request.session_id}
                """);

            var newAccessToken = _jwtTokenService.GenerateAccessToken(currentUser, request.session_id);

            return Ok(new AuthResponse
            {
                access_token = newAccessToken,
                refresh_token = newRefreshToken,
                session_id = request.session_id,
                expires_at_utc = _jwtTokenService.GetAccessTokenExpiryUtc(),
                user = currentUser
            });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(LogoutRequest request)
        {
            var userId = User.GetUserId();

            var exists = await _context.user_sessions.AnyAsync(x => x.id == request.session_id && x.user_id == userId && x.revoked_at == null);
            if (!exists)
                return NotFound(new { message = "Session not found." });

            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE user_sessions
                SET revoked_at = NOW()
                WHERE id = {request.session_id}
                  AND user_id = {userId}
                  AND revoked_at IS NULL
                """);

            return Ok(new { message = "Logged out successfully." });
        }

        private class LoginUserRow
        {
            public long id { get; set; }
            public string full_name { get; set; } = string.Empty;
            public string email { get; set; } = string.Empty;
            public string? username { get; set; }
            public string? phone { get; set; }
            public string? password_hash { get; set; }
            public string status { get; set; } = string.Empty;
            public string? role { get; set; }
        }

        private class MeUserRow
        {
            public long id { get; set; }
            public string full_name { get; set; } = string.Empty;
            public string email { get; set; } = string.Empty;
            public string? username { get; set; }
            public string? phone { get; set; }
            public string status { get; set; } = string.Empty;
            public string? role { get; set; }
        }

        private class RefreshSessionRow
        {
            public long id { get; set; }
            public long user_id { get; set; }
            public string refresh_token_hash { get; set; } = string.Empty;
            public DateTime? expires_at { get; set; }
        }
    }
}