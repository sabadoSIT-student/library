
using System.ComponentModel.DataAnnotations;

namespace library.Models;

// One Book object holds one row of lending.book.
public class Book
{
    public long BookId { get; set; }

    [Required]
    [StringLength(150)]
    public string Title { get; set; } = "";

    public string? Category { get; set; }

    [Range(typeof(decimal), "0.01", "99999.99")]
    public decimal? Price { get; set; }
}
