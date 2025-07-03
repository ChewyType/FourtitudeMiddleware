using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FourtitudeMiddleware.Dtos
{
    public class ItemRequest
    {
        [Required]
        [StringLength(50)]
        [JsonPropertyName("partneritemref")]
        public string PartnerItemRef { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(1, 5)]
        [JsonPropertyName("qty")]
        public int Qty { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        [JsonPropertyName("unitprice")]
        public long UnitPrice { get; set; }
    }
}
