using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace TranThiNga_2380601420_Tuan4_LTWeb.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public string FullName { get; set; }
        public string? Address { get; set; }
        public string? Age { get; set; }
    }
}
