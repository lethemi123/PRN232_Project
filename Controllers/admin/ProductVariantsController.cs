using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using ProjectPRN232.DTO.Reponse;
using ProjectPRN232.Models;

namespace ProjectPRN232.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/product-variants")]
    [Authorize(Roles = "Admin")]
    public class AdminProductVariantsController : ControllerBase
    {
        private readonly Prn212AssignmentContext _context;
        public AdminProductVariantsController(Prn212AssignmentContext context) => _context = context;

        // GET: /api/admin/product-variants?productId=1
        [HttpGet]
        public async Task<IActionResult> GetVariants([FromQuery] int? productId = null)
        {
            var query = _context.ProductVariants
                .Include(v => v.Product)
                .AsQueryable();

            if (productId.HasValue)
                query = query.Where(v => v.ProductId == productId.Value);

            var data = await query
                .Select(v => new VariantRespone
                {
                    VariantId = v.VariantId,
                    ProductId = v.ProductId,
                    ProductName = v.Product.ProductName,
                    VariantName = v.Storage ?? "",
                    Price = v.Price ?? 0,
                    Stock = v.Stock ?? 0,
                    ImageUrl = v.Product.ImagePathProduct ?? ""
                })
                .ToListAsync();

            return Ok(data);
        }

        // GET: /api/admin/product-variants/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _context.ProductVariants
                .Include(v => v.Product)
                .Where(v => v.VariantId == id)
                .Select(v => new VariantRespone
                {
                    VariantId = v.VariantId,
                    ProductId = v.ProductId,
                    ProductName = v.Product.ProductName,
                    VariantName = v.Storage ?? "",
                    Price = v.Price ?? 0,
                    Stock = v.Stock ?? 0,
                    ImageUrl = v.Product.ImagePathProduct ?? ""
                })
                .FirstOrDefaultAsync();

            if (item == null) return NotFound(new { message = "Variant not found" });
            return Ok(item);
        }

        // POST: /api/admin/product-variants
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] VariantUpsertRequest req)
        {
            var product = await _context.Products.FindAsync(req.ProductId);
            if (product == null)
                return BadRequest(new { message = "Product not found" });

            var v = new ProductVariant
            {
                ProductId = req.ProductId,
                Storage = req.VariantName,
                Price = req.Price,
                Stock = req.Stock
            };

            _context.ProductVariants.Add(v);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Created", v.VariantId });
        }

        // PUT: /api/admin/product-variants/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] VariantUpsertRequest req)
        {
            var v = await _context.ProductVariants.FindAsync(id);
            if (v == null) return NotFound(new { message = "Variant not found" });

            v.Storage = req.VariantName;
            v.Price = req.Price;
            v.Stock = req.Stock;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Updated" });
        }

        // DELETE: /api/admin/product-variants/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var v = await _context.ProductVariants.FindAsync(id);
            if (v == null) return NotFound(new { message = "Variant not found" });

            _context.ProductVariants.Remove(v);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deleted" });
        }
    }
}
