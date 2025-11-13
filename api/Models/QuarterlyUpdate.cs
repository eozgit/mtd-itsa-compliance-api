using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace api.Models;

public class QuarterlyUpdate
{
    // MongoDB uses ObjectId for _id by default, but we can map a string.
    // We'll use a string for consistency with other IDs and easier querying.
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    // Foreign key to the SQL Server Business Id
    public int BusinessId { get; set; }

    public string TaxYear { get; set; } = string.Empty; // e.g., "2025/26"
    public string QuarterName { get; set; } = string.Empty; // e.g., "Q1"

    public decimal TaxableIncome { get; set; } = 0.00m;
    public decimal AllowableExpenses { get; set; } = 0.00m;

    // Calculated fields (data enrichment)
    public decimal NetProfit { get; set; } = 0.00m; // Calculated: TaxableIncome - AllowableExpenses

    public string Status { get; set; } = "DRAFT"; // DRAFT, SUBMITTED

    // Submission details, only present if status is SUBMITTED
    public SubmissionDetails? SubmissionDetails { get; set; }
}

public class SubmissionDetails
{
    public string RefNumber { get; set; } = string.Empty; // e.g., "MTD-ACK-..."
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
