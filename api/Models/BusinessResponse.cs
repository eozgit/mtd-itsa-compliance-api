namespace api.Models;

public class BusinessResponse
{
    public int BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;

    public BusinessResponse(int businessId, string name)
    {
        BusinessId = businessId;
        Name = name;
    }
}
