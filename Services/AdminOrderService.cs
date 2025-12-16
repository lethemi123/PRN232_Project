using Microsoft.EntityFrameworkCore;
using ProjectPRN232.Models;

namespace ProjectPRN232.Services
{
    public class AdminOrderService
    {
        private readonly Prn212AssignmentContext _context;
        public AdminOrderService(Prn212AssignmentContext context) => _context = context;

        public async Task<object> GetOrders(string? status)
        {
            var query = _context.Orders.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.OrderStatus == status);

            return await query
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.OrderId,
                    o.PersonId,
                    o.OrderDate,
                    o.TotalMoney,
                    o.OrderStatus,
                    o.PaymentMethod,
                    o.ReceiverName,
                    o.ReceiverPhone,
                    o.ReceiverAddress
                })
                .ToListAsync();
        }

        public async Task Approve(string orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null) throw new Exception("Order not found.");
            if (order.OrderStatus != "Pending") throw new Exception("Chỉ duyệt được đơn Pending.");

            order.OrderStatus = "Approved";
            await _context.SaveChangesAsync();
        }

        public async Task RejectAndRefund(string orderId)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order == null) throw new Exception("Order not found.");
            if (order.OrderStatus != "Pending") throw new Exception("Chỉ từ chối được đơn Pending.");
            if (order.PersonId == null) throw new Exception("Order lỗi: PersonId null.");

            var user = await _context.People.FirstOrDefaultAsync(p => p.PersonId == order.PersonId.Value);
            if (user == null) throw new Exception("User not found.");

            var details = await _context.OrderDetails.Where(d => d.OrderId == orderId).ToListAsync();
            if (!details.Any()) throw new Exception("OrderDetail rỗng.");

            // refund
            var refund = order.TotalMoney ?? 0.0;
            user.Balance = (user.Balance ?? 0.0) + refund;

            // restock
            var variantIds = details.Where(d => d.VariantId != null).Select(d => d.VariantId!.Value).Distinct().ToList();
            var variants = await _context.ProductVariants
                .Where(v => variantIds.Contains(v.VariantId))
                .ToDictionaryAsync(v => v.VariantId);

            foreach (var d in details)
            {
                if (d.VariantId == null) continue;
                var qty = d.Quantity ?? 0;
                if (variants.TryGetValue(d.VariantId.Value, out var v))
                    v.Stock = (v.Stock ?? 0) + qty;
            }

            order.OrderStatus = "Rejected";

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
    }
}
