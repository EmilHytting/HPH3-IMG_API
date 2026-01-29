namespace HPH3_IMG_API.Models;

public class Category
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
