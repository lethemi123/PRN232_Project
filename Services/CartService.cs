using Microsoft.EntityFrameworkCore;
using ProjectPRN232.Models;

namespace ProjectPRN232.Services
{
    public class CartService
    {
        private readonly Prn212AssignmentContext _context;
        public CartService(Prn212AssignmentContext context) => _context = context;

        // Add theo VariantId (backend tự suy ra ProductId từ Variant)
        public async Task AddToCart(int userId, int variantId, int quantity)
        {
            if (quantity <= 0) throw new Exception("Quantity phải > 0");

            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.VariantId == variantId);

            if (variant == null) throw new Exception("Variant không tồn tại");

            var stock = variant.Stock ?? 0;
            if (stock < quantity) throw new Exception("Không đủ hàng trong kho");

            var productId = variant.ProductId;

            // Cart: PersonId/VariantId/Quantity là nullable => xử lý cẩn thận
            var item = await _context.Carts.FirstOrDefaultAsync(c =>
                c.PersonId == userId && c.VariantId == variantId);

            if (item == null)
            {
                _context.Carts.Add(new Cart
                {
                    PersonId = userId,
                    ProductId = productId,
                    VariantId = variantId,
                    Quantity = quantity,
                    IsSelected = false
                });
            }
            else
            {
                var currentQty = item.Quantity ?? 0;
                var newQty = currentQty + quantity;

                if (stock < newQty) throw new Exception("Không đủ hàng trong kho");

                item.Quantity = newQty;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<object> GetCart(int userId)
        {
            var items = await (from c in _context.Carts
                               join v in _context.ProductVariants on c.VariantId equals v.VariantId
                               join p in _context.Products on c.ProductId equals p.ProductId
                               where c.PersonId == userId
                               select new
                               {
                                   c.CartId,
                                   c.ProductId,
                                   p.ProductName,
                                   p.ImagePathProduct,
                                   VariantId = c.VariantId,
                                   v.Storage,
                                   Price = v.Price ?? 0m,
                                   Quantity = c.Quantity ?? 0,
                                   IsSelected = c.IsSelected ?? false,
                                   LineTotal = (v.Price ?? 0m) * (c.Quantity ?? 0)
                               }).ToListAsync();

            var total = items.Sum(x => x.LineTotal);
            var selectedTotal = items.Where(x => x.IsSelected).Sum(x => x.LineTotal);

            return new
            {
                items,
                total,
                selectedTotal
            };
        }

        public async Task RemoveItem(int userId, int cartId)
        {
            var item = await _context.Carts
                .FirstOrDefaultAsync(c => c.CartId == cartId && c.PersonId == userId);

            if (item == null) return;

            _context.Carts.Remove(item);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateQuantity(int userId, int cartId, int quantity)
        {
            if (quantity <= 0) throw new Exception("Quantity phải > 0");

            var item = await _context.Carts
                .FirstOrDefaultAsync(c => c.CartId == cartId && c.PersonId == userId);

            if (item == null) throw new Exception("Cart item not found.");

            if (item.VariantId == null) throw new Exception("Cart item chưa chọn variant.");

            var variant = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.VariantId == item.VariantId.Value);

            if (variant == null) throw new Exception("Variant không tồn tại.");

            var stock = variant.Stock ?? 0;
            if (stock < quantity) throw new Exception("Không đủ hàng trong kho");

            item.Quantity = quantity;
            await _context.SaveChangesAsync();
        }

        public async Task ToggleSelect(int userId, int cartId, bool isSelected)
        {
            var item = await _context.Carts
                .FirstOrDefaultAsync(c => c.CartId == cartId && c.PersonId == userId);

            if (item == null) throw new Exception("Cart item not found.");

            item.IsSelected = isSelected;
            await _context.SaveChangesAsync();
        }
    }
}
