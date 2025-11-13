namespace api.Models;

public class QuarterSubmissionResponse
{
    public string Id { get; set; } = string.Empty;
    public int BusinessId { get; set; }
    public string TaxYear { get; set; } = string.Empty;
    public string QuarterName { get; set; } = string.Empty;
    public decimal TaxableIncome { get; set; }
    public decimal AllowableExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public string Status { get; set; } = string.Empty;
    public SubmissionDetails? SubmissionDetails { get; set; } // Nested DTO
    public string Message { get; set; } = string.Empty; // Matches 'Message' from the API response
}
