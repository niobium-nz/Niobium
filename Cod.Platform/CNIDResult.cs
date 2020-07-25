using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Cod.Platform
{
    public class CNIDResult
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Gender { get; set; }
        [Required]
        public string Nationality { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        [StringLength(8, MinimumLength = 8, ErrorMessage = "生日错误")]
        public string Birthday { get; set; }
        [Required]
        [StringLength(18, MinimumLength = 18, ErrorMessage = "身份证号码错误")]
        public string Number { get; set; }
    }
}
