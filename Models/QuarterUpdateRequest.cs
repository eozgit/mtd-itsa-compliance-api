
namespace api.Models;

public class QuarterlyUpdateRequest
{
    public decimal TaxableIncome { get; set; } = 0.00m;
    public decimal AllowableExpenses { get; set; } = 0.00m;
}
