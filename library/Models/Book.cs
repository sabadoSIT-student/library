
namespace library.Models;

// One Book object holds one row of lending.book.
public class Book
{
    public long BookId { get; set; }
    public string Title { get; set; } = "";
    public string? Category { get; set; }
    public decimal? Price { get; set; }
}