using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectPRN232.DTO.Cart;
using ProjectPRN232.Services;
using System.Security.Claims;

namespace ProjectPRN232.Controllers
{
    [ApiController]
    [Route("api/cart")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly CartService _service;
        public CartController(CartService service) => _service = service;

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Add theo VariantId (user chọn 125/256 rồi add)
        [HttpPost("items")]
        public async Task<IActionResult> Add([FromBody] AddToCartRequest req)
        {
            try
            {
                await _service.AddToCart(UserId, req.variantId, req.quantity);
                return Ok(new { message = "Added to cart" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var data = await _service.GetCart(UserId);
            return Ok(data);
        }

        [HttpDelete("items/{cartId:int}")]
        public async Task<IActionResult> Remove(int cartId)
        {
            await _service.RemoveItem(UserId, cartId);
            return Ok(new { message = "Removed" });
        }

        [HttpPut("items/{cartId:int}/quantity")]
        public async Task<IActionResult> UpdateQty(int cartId, [FromBody] UpdateCartQtyRequest req)
        {
            try
            {
                await _service.UpdateQuantity(UserId, cartId, req.Quantity);
                return Ok(new { message = "Updated quantity" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("items/{cartId:int}/select")]
        public async Task<IActionResult> ToggleSelect(int cartId, [FromBody] ToggleSelectRequest req)
        {
            try
            {
                await _service.ToggleSelect(UserId, cartId, req.IsSelected);
                return Ok(new { message = "Updated selection" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
