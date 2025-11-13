
namespace api.Models;

public class QuartersResponse
{
    public List<QuarterlyUpdate> Quarters { get; set; } = new List<QuarterlyUpdate>();
    public decimal CumulativeEstimatedTaxLiability { get; set; }
    public decimal TotalNetProfitSubmitted { get; set; }
}
