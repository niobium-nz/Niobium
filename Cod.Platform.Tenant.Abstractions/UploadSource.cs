using System.ComponentModel.DataAnnotations;

namespace Cod.Platform.Tenant
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
