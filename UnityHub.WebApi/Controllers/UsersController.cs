using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UnityHub.Application.DTOs;
using UnityHub.Application.Interfaces.Services;

namespace UnityHub.WebApi.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }
        private long GetUserIdFromToken()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            return userIdClaim != null ? long.Parse(userIdClaim.Value) : 0;
        }
        // GET: api/users
        [Authorize]
        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var result = await _userService.GetAllAsync(pageNumber, pageSize, startDate, endDate);
            return Ok(result);
        }

        // GET: api/users/{id}
        [Authorize]
        [HttpGet("GetById/{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();
            return Ok(user);
        }

        // POST: api/users
        [Authorize]
        [HttpPost("Create")]
        public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
        {
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin)
                return BadRequest(new{message= "Only admins can perform this action."});
            dto.CreatedBy = GetUserIdFromToken();

            await _userService.CreateAsync(dto);
            return Ok(new { message = "User created successfully with Roles" });
        }

        // PUT: api/users/{id}
        [Authorize]
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] UserUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("User ID mismatch");
            dto.UpdatedBy = GetUserIdFromToken();
            await _userService.UpdateAsync(dto);
            return Ok(new { message = "User updated successfully" });
        }

        // DELETE: api/users/{id}?updatedBy=123
        [Authorize]
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var updatedBy = GetUserIdFromToken();  // ✅ set deleter ID
            await _userService.DeleteAsync(id, updatedBy);
            return Ok(new { message = "User deleted successfully" });
        }
    }
}
