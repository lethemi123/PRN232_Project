using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectPRN232.Services;

namespace ProjectPRN232.Controllers
{
    [ApiController]
    [Route("api/admin/orders")]
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly AdminOrderService _service;
        public AdminOrdersController(AdminOrderService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> GetOrders([FromQuery] string? status = null)
            => Ok(await _service.GetOrders(status));

        [HttpPut("{orderId}/approve")]
        public async Task<IActionResult> Approve(string orderId)
        {
            await _service.Approve(orderId);
            return Ok(new { message = "Approved" });
        }

        [HttpPut("{orderId}/reject")]
        public async Task<IActionResult> Reject(string orderId)
        {
            await _service.RejectAndRefund(orderId);
            return Ok(new { message = "Rejected + refunded" });
        }
    }
}
