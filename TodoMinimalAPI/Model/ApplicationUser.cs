using Microsoft.AspNetCore.Identity;

namespace TodoMinimalAPI.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string  FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? DOB { get; set; }
    }
}
