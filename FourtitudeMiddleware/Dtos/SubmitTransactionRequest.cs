using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FourtitudeMiddleware.Dtos
{
    public class SubmitTransactionRequest
    {
        [Required]
        [StringLength(50)]
        [JsonPropertyName("partnerkey")]
        public string PartnerKey { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [JsonPropertyName("partnerrefno")]
        public string PartnerRefNo { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [JsonPropertyName("partnerpassword")]
        public string PartnerPassword { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("totalamount")]
        public long TotalAmount { get; set; }

        [Required]
        [JsonPropertyName("items")]
        public List<ItemRequest> Items { get; set; } = new();

        [Required]
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("sig")]
        public string Sig { get; set; } = string.Empty;
    }
}
