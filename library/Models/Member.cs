
using System.ComponentModel.DataAnnotations;

namespace library.Models;

public class Member
{
    public long MemberId { get; set; }

    [Required]
    [StringLength(80)]
    [RegularExpression(@"^[A-Za-zÑñ .'-]+, [A-Za-zÑñ .'-]+$",
        ErrorMessage = "Enter the name in Last Name, First Name format.")]
    public string FullName { get; set; } = "";

    [Required]
    [StringLength(120)]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[A-Za-z]{2,}$",
        ErrorMessage = "Enter a valid email address.")]
    public string? Email { get; set; }

    public string MemberType { get; set; } = "";
}
