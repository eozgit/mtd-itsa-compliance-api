using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System; // Ensure System is imported for DateTime

namespace api.Models
{
    public class QuarterlyUpdate
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public int BusinessId { get; set; }
        public string TaxYear { get; set; } = string.Empty;
        public string QuarterName { get; set; } = string.Empty;
        public decimal TaxableIncome { get; set; }
        public decimal AllowableExpenses { get; set; }
        public decimal NetProfit { get; set; } // Calculated from TaxableIncome - AllowableExpenses
        public string Status { get; set; } = "DRAFT"; // DRAFT, SUBMITTED
        public SubmissionDetails? SubmissionDetails { get; set; }
    }

    public class SubmissionDetails
    {
        public string RefNumber { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
    }
}
