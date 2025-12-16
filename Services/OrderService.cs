using Microsoft.EntityFrameworkCore;
using ProjectPRN232.DTO.Order;
using ProjectPRN232.Models;

namespace ProjectPRN232.Services
{
    public class OrderService
    {
        private readonly Prn212AssignmentContext _context;
        public OrderService(Prn212AssignmentContext context) => _context = context;

        public async Task<string> CheckoutSelected(int userId, CheckoutRequest req)
        {
            if (req.CartItemIds == null || req.CartItemIds.Count == 0)
                throw new Exception("Bạn chưa chọn sản phẩm để thanh toán.");

            using var tx = await _context.Database.BeginTransactionAsync();

            var user = await _context.People.FirstOrDefaultAsync(p => p.PersonId == userId);
            if (user == null) throw new Exception("User không tồn tại");

            var selectedCart = await _context.Carts
                .Where(c => c.PersonId == userId && req.CartItemIds.Contains(c.CartId))
                .ToListAsync();

            if (!selectedCart.Any())
                throw new Exception("Không tìm thấy sản phẩm đã chọn trong giỏ hàng.");

            foreach (var c in selectedCart)
            {
                if (c.VariantId == null) throw new Exception($"CartId={c.CartId} chưa chọn Variant.");
                if (c.Quantity == null || c.Quantity <= 0) throw new Exception($"CartId={c.CartId} số lượng không hợp lệ.");
            }

            var variantIds = selectedCart.Select(x => x.VariantId!.Value).Distinct().ToList();

            var variants = await _context.ProductVariants
                .Where(v => variantIds.Contains(v.VariantId))
                .ToDictionaryAsync(v => v.VariantId);

            decimal total = 0m;

            foreach (var item in selectedCart)
            {
                var vid = item.VariantId!.Value;
                var qty = item.Quantity!.Value;

                if (!variants.TryGetValue(vid, out var v))
                    throw new Exception($"VariantId={vid} không tồn tại");

                var stock = v.Stock ?? 0;
                if (stock < qty)
                    throw new Exception($"Không đủ hàng cho VariantId={vid}. Còn {stock}.");

                var price = v.Price ?? 0m;
                total += price * qty;
            }

            var balance = (decimal)(user.Balance ?? 0.0);
            if (balance < total)
            {
                var thieu = total - balance;
                throw new Exception($"Số dư không đủ. Thiếu: {thieu}");
            }

            // trừ tiền + trừ stock
            user.Balance = (double)(balance - total);

            foreach (var item in selectedCart)
            {
                var vid = item.VariantId!.Value;
                var qty = item.Quantity!.Value;
                variants[vid].Stock = (variants[vid].Stock ?? 0) - qty;
            }

            var orderId = Guid.NewGuid().ToString("N");

            var order = new Order
            {
                OrderId = orderId,
                PersonId = userId,
                OrderDate = DateTime.UtcNow,
                TotalMoney = (double)total,
                OrderStatus = "Pending",
                PaymentMethod = req.PaymentMethod,
                ReceiverName = req.ReceiverName,
                ReceiverPhone = req.ReceiverPhone,
                ReceiverAddress = req.ReceiverAddress,
                OrderAddress = req.ReceiverAddress
            };

            _context.Orders.Add(order);

            foreach (var item in selectedCart)
            {
                var vid = item.VariantId!.Value;
                var qty = item.Quantity!.Value;
                var v = variants[vid];

                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderDetailId = Guid.NewGuid().ToString("N"),
                    OrderId = orderId,
                    ProductId = item.ProductId,
                    VariantId = vid,
                    Quantity = qty,
                    UnitPrice = v.Price ?? 0m
                });
            }

            _context.Carts.RemoveRange(selectedCart);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return orderId;
        }
    }
}
