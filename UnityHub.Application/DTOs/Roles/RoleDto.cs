namespace UnityHub.Application.DTOs
{
    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsStatic { get; set; }
        public bool IsDefault { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class RoleCreateDto
    {
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }

    public class RoleUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
    }
    public class UserRoleDto
    {
        public long UserId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = default!;
        public bool IsDeleted { get; set; }
    }

}
