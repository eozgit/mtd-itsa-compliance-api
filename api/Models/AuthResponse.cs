
namespace api.Models;

public class AuthResponse
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;

    public AuthResponse(string userId, string userName, string token)
    {
        UserId = userId;
        UserName = userName;
        Token = token;
    }
}
