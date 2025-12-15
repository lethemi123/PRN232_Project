using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectPRN232.DTO.Order;
using ProjectPRN232.Services;
using System.Security.Claims;

namespace ProjectPRN232.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly OrderService _service;
        public OrdersController(OrderService service) => _service = service;

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("checkout-selected")]
        public async Task<IActionResult> CheckoutSelected([FromBody] CheckoutRequest req)
        {
            try
            {
                var orderId = await _service.CheckoutSelected(UserId, req);
                return Ok(new { message = "Checkout success", orderId, status = "Pending" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
