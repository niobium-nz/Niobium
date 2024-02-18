using System.ComponentModel.DataAnnotations;

namespace Cod.Platform.OCR
{
    public class ChineseIDInfo
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public Gender Gender { get; set; }

        [Required]
        public string Race { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public DateTimeOffset Birthday { get; set; }

        [Required]
        [StringLength(18, MinimumLength = 18)]
        public string Number { get; set; }

        [Required]
        public string Issuer { get; set; }

        [Required]
        public DateTimeOffset Expiry { get; set; }
    }
}
