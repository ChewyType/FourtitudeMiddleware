using System.ComponentModel.DataAnnotations;

namespace FourtitudeMiddleware.Dtos
{
    public class ItemRequest
    {
        [Required]
        [StringLength(50)]
        public string PartnerItemRef { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(1, 5)]
        public int Qty { get; set; }

        [Required]
        [Range(1, long.MaxValue)]
        public long UnitPrice { get; set; }
    }
}
