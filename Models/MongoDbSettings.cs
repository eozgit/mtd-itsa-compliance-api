namespace api.Models;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string QuarterlyUpdatesCollectionName { get; set; } = string.Empty;
}
