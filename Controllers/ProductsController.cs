using HPH3_IMG_API.Data;
using HPH3_IMG_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HPH3_IMG_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        return await _context.Products.Include(p => p.Category).ToListAsync();
    }

    // GET: api/products/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        return product;
    }

    // GET: api/products/category/5
    [HttpGet("category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(int categoryId)
    {
        var products = await _context.Products
            .Where(p => p.CategoryId == categoryId)
            .Include(p => p.Category)
            .ToListAsync();

        return products;
    }

    // POST: api/products
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(CreateProductDto createProductDto)
    {
        // Verify category exists
        var category = await _context.Categories.FindAsync(createProductDto.CategoryId);
        if (category == null)
        {
            return BadRequest("Category not found");
        }

        var product = new Product
        {
            Title = createProductDto.Title,
            Description = createProductDto.Description,
            Price = createProductDto.Price,
            ImageUrl = createProductDto.ImageUrl,
            CategoryId = createProductDto.CategoryId
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }

    // PUT: api/products/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, UpdateProductDto updateProductDto)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        if (updateProductDto.CategoryId.HasValue)
        {
            var category = await _context.Categories.FindAsync(updateProductDto.CategoryId.Value);
            if (category == null)
            {
                return BadRequest("Category not found");
            }
            product.CategoryId = updateProductDto.CategoryId.Value;
        }

        product.Title = updateProductDto.Title ?? product.Title;
        product.Description = updateProductDto.Description ?? product.Description;
        if (updateProductDto.Price.HasValue)
        {
            product.Price = updateProductDto.Price.Value;
        }
        product.ImageUrl = updateProductDto.ImageUrl ?? product.ImageUrl;
        product.UpdatedAt = DateTime.UtcNow;

        _context.Products.Update(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/products/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class CreateProductDto
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public required string ImageUrl { get; set; }
    public int CategoryId { get; set; }
}

public class UpdateProductDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? ImageUrl { get; set; }
    public int? CategoryId { get; set; }
}
