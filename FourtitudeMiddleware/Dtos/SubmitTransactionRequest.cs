using System.ComponentModel.DataAnnotations;

namespace FourtitudeMiddleware.Dtos
{
    public class SubmitTransactionRequest
    {
        [Required]
        [StringLength(50)]
        public string PartnerKey { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string PartnerRefNo { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string PartnerPassword { get; set; } = string.Empty;

        [Required]
        public long TotalAmount { get; set; }

        public List<ItemRequest> Items { get; set; }

        [Required]
        public string Timestamp { get; set; } = string.Empty;

        [Required]
        public string Sig { get; set; } = string.Empty;
    }
}
