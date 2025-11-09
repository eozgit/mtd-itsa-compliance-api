
namespace api.Models;

public class Business
{
    public int Id { get; set; } // Primary Key for Business
    public string UserId { get; set; } = string.Empty; // Foreign Key to User.Id
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property for Entity Framework (optional, but good for relationships)
    public User User { get; set; } = null!;
}
