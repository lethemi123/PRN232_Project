using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPRN232.DTO.Order;
using ProjectPRN232.Models;
using ProjectPRN232.Services;
using System.Security.Claims;

namespace ProjectPRN232.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly Prn212AssignmentContext _context;
        private readonly OrderService _orderService;

        public OrdersController(Prn212AssignmentContext context, OrderService orderService)
        {
            _context = context;
            _orderService = orderService;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("checkout-selected")]
        public async Task<IActionResult> CheckoutSelected([FromBody] CheckoutRequest req)
        {
            try
            {
                var orderId = await _orderService.CheckoutSelected(UserId, req);
                return Ok(new { message = "Checkout success", orderId, status = "Pending" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET /api/orders/my?status=Pending
        [HttpGet("my")]
        public async Task<IActionResult> MyOrders([FromQuery] string? status = null)
        {
            var query = _context.Orders.Where(o => o.PersonId == UserId);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.OrderStatus == status);

            var data = await query
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderDate,
                    o.TotalMoney,
                    o.OrderStatus,
                    o.PaymentMethod,
                    o.ReceiverName,
                    o.ReceiverPhone,
                    o.ReceiverAddress
                })
                .ToListAsync();

            return Ok(data);
        }

        // GET /api/orders/my/{orderId}
        [HttpGet("my/{orderId}")]
        public async Task<IActionResult> MyOrderDetail(string orderId)
        {
            var order = await _context.Orders
                .Where(o => o.OrderId == orderId && o.PersonId == UserId)
                .Select(o => new
                {
                    o.OrderId,
                    o.OrderDate,
                    o.TotalMoney,
                    o.OrderStatus,
                    o.PaymentMethod,
                    o.ReceiverName,
                    o.ReceiverPhone,
                    o.ReceiverAddress,
                    Items = _context.OrderDetails
                        .Where(d => d.OrderId == o.OrderId)
                        .Join(_context.ProductVariants, d => d.VariantId, v => v.VariantId, (d, v) => new { d, v })
                        .Join(_context.Products, x => x.v.ProductId, p => p.ProductId, (x, p) => new
                        {
                            x.d.OrderDetailId,
                            p.ProductId,
                            p.ProductName,
                            x.d.VariantId,
                            x.v.Storage,
                            Quantity = x.d.Quantity ?? 0,
                            x.d.UnitPrice,
                            LineTotal = x.d.UnitPrice * (x.d.Quantity ?? 0)
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (order == null) return NotFound(new { message = "Order not found." });
            return Ok(order);
        }

        // PUT /api/orders/my/{orderId}/cancel
        [HttpPut("my/{orderId}/cancel")]
        public async Task<IActionResult> CancelMyOrder(string orderId)
        {
            using var tx = await _context.Database.BeginTransactionAsync();

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId && o.PersonId == UserId);
            if (order == null) return NotFound(new { message = "Order not found." });

            if (order.OrderStatus != "Pending")
                return BadRequest(new { message = "Chỉ hủy được đơn Pending." });

            var user = await _context.People.FirstOrDefaultAsync(p => p.PersonId == UserId);
            if (user == null) return BadRequest(new { message = "User not found." });

            var details = await _context.OrderDetails.Where(d => d.OrderId == orderId).ToListAsync();
            if (!details.Any()) return BadRequest(new { message = "OrderDetail rỗng." });

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

            order.OrderStatus = "Canceled";

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { message = "Canceled + refunded", orderId, status = "Canceled" });
        }
    }
}
