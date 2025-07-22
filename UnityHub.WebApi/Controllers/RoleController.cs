using Microsoft.AspNetCore.Mvc;
using UnityHub.Application.DTOs;
using UnityHub.Application.Interfaces.Services;
using UnityHub.Application.Services;

namespace UnityHub.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _service;

        public RoleController(IRoleService service)
        {
            _service = service;
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(int page = 1, int size = 10, DateTime? start = null, DateTime? end = null)
        {
            return Ok(await _service.GetPagedAsync(page, size, start, end));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var role = await _service.GetByIdAsync(id);
            if (role == null) return NotFound();
            return Ok(role);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoleCreateDto dto)
        {
            long createdBy = 1; // Replace with JWT userId
            await _service.CreateAsync(dto);
            return Ok("Role created successfully");
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] RoleUpdateDto dto)
        {
            long updatedBy = 1; // Replace with JWT userId
            await _service.UpdateAsync(dto);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            long updatedBy = 1; // Replace with JWT userId
            await _service.DeleteAsync(id);
            return Ok();
        }
    }
}
