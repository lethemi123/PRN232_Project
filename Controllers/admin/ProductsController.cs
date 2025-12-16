using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ProjectPRN232.DTO.Reponse;
using ProjectPRN232.Models;

namespace ProjectPRN232.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/products")]
    [Authorize(Roles = "Admin")]
    public class AdminProductsController : ControllerBase
    {
        private readonly Prn212AssignmentContext _context;
        public AdminProductsController(Prn212AssignmentContext context) => _context = context;

        // GET: /api/admin/products?search=iphone
        [HttpGet("ListProduct")]
        public async Task<IActionResult> GetProducts([FromQuery] string? search = null)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(s));
            }

            var listItems = await query
                .Select(p => new ProductRespone
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName,
                    Description = p.ProductDescription ?? "",
                    ImageUrl = p.ImagePathProduct ?? "",
                    CategoryName = p.Category != null ? p.Category.CategoryName : ""
                })
                .ToListAsync();

            return Ok(listItems);
        }

 
        
        // POST: /api/admin/products
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProductUpsertRequest req)
        {
            // check category tồn tại
            var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == req.CategoryId);
            if (!categoryExists) return BadRequest(new { message = "Category not found" });

            var p = new Product
            {
                ProductName = req.ProductName,
                ProductDescription = req.Description,
                ImagePathProduct = req.ImageUrl,
                CategoryId = req.CategoryId,
             
            };

            _context.Products.Add(p);
            await _context.SaveChangesAsync();

           return CreatedAtAction(nameof(GetProducts), new { id = p.ProductId }, new { message = "Created", productId = p.ProductId });
        }

        // PUT: /api/admin/products/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProductUpsertRequest req)
        {
            var p = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == id);
            if (p == null) return NotFound(new { message = "Product not found" });

            var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId.Equals(req.CategoryId));
            if (!categoryExists) return BadRequest(new { message = "Category not found" });

            p.ProductName = req.ProductName;
            p.ProductDescription = req.Description;
            p.ImagePathProduct = req.ImageUrl;
            p.CategoryId = req.CategoryId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Updated" });
        }

        // DELETE: /api/admin/products/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == id);
            if (p == null) return NotFound(new { message = "Product not found" });

            _context.Products.Remove(p);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted" });
        }
    }
}
