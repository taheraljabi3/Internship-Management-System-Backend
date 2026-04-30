namespace IMS.Api.DTOs
{
    public class LoginRequest
    {
        public string login { get; set; } = string.Empty; // email or username
        public string password { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        public long session_id { get; set; }
        public string refresh_token { get; set; } = string.Empty;
    }

    public class LogoutRequest
    {
        public long session_id { get; set; }
    }

    public class BootstrapAdminRequest
    {
        public string full_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;
        public string? username { get; set; }
        public string? phone { get; set; }
        public string? employee_no { get; set; }
        public string? department { get; set; }
    }

    public class CurrentUserDto
    {
        public long id { get; set; }
        public string full_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string? username { get; set; }
        public string? phone { get; set; }
        public string role { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string access_token { get; set; } = string.Empty;
        public string refresh_token { get; set; } = string.Empty;
        public long session_id { get; set; }
        public DateTime expires_at_utc { get; set; }
        public CurrentUserDto user { get; set; } = new();
    }
}