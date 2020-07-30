using System.ComponentModel.DataAnnotations;

namespace Cod
{
    public class UploadSource
    {
        [Required]
        public string AppID { get; set; }

        public OpenIDKind OpenIDKind { get; set; }

        [Required]
        public string FileID { get; set; }
    }
}
