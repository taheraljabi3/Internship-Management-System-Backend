namespace IMS.Api.DTOs
{
    public class UserListItemDto
    {
        public long id { get; set; }
        public string full_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string? phone { get; set; }
        public string? username { get; set; }
        public string? role { get; set; }
        public string status { get; set; } = string.Empty;
    }

    public class CreateUserRequest
    {
        public string full_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string password { get; set; } = string.Empty;

        public string? username { get; set; }
        public string? phone { get; set; }

        public string role_code { get; set; } = string.Empty;
        public string status { get; set; } = "Active";
        public long? created_by_user_id { get; set; }

        // Student only
        public string? student_code { get; set; }
        public string? university { get; set; }
        public string? major { get; set; }
        public long? advisor_user_id { get; set; }

        // Staff only
        public string? employee_no { get; set; }
        public string? department { get; set; }
        public bool is_system_responsible { get; set; }
    }

    public class UpdateUserRequest
    {
        public string full_name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string? username { get; set; }
        public string? phone { get; set; }
        public string status { get; set; } = "Active";
    }
}