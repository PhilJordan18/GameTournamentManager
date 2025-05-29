namespace Core.Entities;

public class User
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public Role Role { get; set; }

    public string Username { get; set; }
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }

    public string PhoneNumber { get; set; }
    public bool Is2FAActived { get; set; }
    public string TwoFactorSecretKey { get; set; }
    public string TwoFactorRecoveryCodes { get; set; }
    public ICollection<UserFcmToken> FcmTokens { get; set; } = [];
}



public class Role
{
    public int Id { get; set; }
    public string Name { get; set; }
}