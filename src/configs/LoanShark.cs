using System.Text.Json.Serialization;

namespace RollTheDice.Configs
{
    public class LoanSharkConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("loan_amount")] public int LoanAmount { get; set; } = 50000;
    }
}
