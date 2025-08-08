using Microsoft.AspNetCore.Mvc;
using UnityHub.Application.Interfaces.Repositories;
using UnityHub.Domain.Entities;

namespace UnityHub.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogRepository _auditLogRepository;

        public AuditLogsController(IAuditLogRepository auditLogRepository)
        {
            _auditLogRepository = auditLogRepository;
        }

        // GET: api/AuditLogs
        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var logs = await _auditLogRepository.GetAllAsync();
            return Ok(logs);
        }
    }
}
