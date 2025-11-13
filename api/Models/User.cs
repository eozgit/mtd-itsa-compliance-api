namespace api.Models;

public class User // Changed from record to class
{
    // Id will be the primary key, matching the spec's UserId as a unique string identifier
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // To store hashed passwords

    // Parameterless constructor for EF Core
    public User() { }

    public User(string id, string email, string userName, string passwordHash)
    {
        Id = id;
        Email = email;
        UserName = userName;
        PasswordHash = passwordHash;
    }
}
