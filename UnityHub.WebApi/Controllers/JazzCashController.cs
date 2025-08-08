// File: UnityHub.Api/Controllers/JazzCashController.cs
using Microsoft.AspNetCore.Mvc;
using UnityHub.Domain.Entities.JazzCash; // To use GetIbanRequest and GetIbanResponse
using UnityHub.Infrastructure.Interfaces; // To inject IJazzCashRaastService
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

namespace UnityHub.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JazzCashController : ControllerBase
    {
        private readonly IJazzCashRaastService _jazzCashRaastService;
        private readonly ILogger<JazzCashController> _logger;

        public JazzCashController(IJazzCashRaastService jazzCashRaastService, ILogger<JazzCashController> logger)
        {
            _jazzCashRaastService = jazzCashRaastService;
            _logger = logger;
        }

        /// <summary>
        /// Initiates a JazzCash Raast Get IBAN request and returns a static response,
        /// using the MSISDN from the provided request.
        /// </summary>
        /// <param name="request">The GetIbanRequest containing the MSISDN and other details.</param>
        /// <returns>The static IBAN details if successful, or an error message.</returns>
        [HttpPost("get-iban-static-mock")] // Changed endpoint name for clarity
        public async Task<IActionResult> GetIbanStaticMock([FromBody] GetIbanRequest request) // Now accepts request from body
        {
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("GetIbanStaticMock: Received null request body.");
                    return BadRequest(new { Error = "Request body cannot be empty." });
                }

                _logger.LogInformation("Received request for static JazzCash Get IBAN for MSISDN: {Msisdn}. Calling Infrastructure service.", request.Msisdn);

                // Pass the incoming request to the infrastructure service
                var response = await _jazzCashRaastService.GetIbanAsync(request);

                if (!response.Success && response.ResponseDetail?.ResponseCode == "01") // Example for specific validation error
                {
                    return BadRequest(new { Error = response.ResponseDetail.ResponseDescription });
                }

                _logger.LogInformation("Static JazzCash Get IBAN mock call completed. Returning result.");
                return Ok(response); // Return 200 OK with the API response object
            }
            catch (ApplicationException appEx)
            {
                _logger.LogError(appEx, "Application error during static JazzCash Get IBAN mock request: {Message}", appEx.Message);
                return StatusCode(500, new { Error = "An error occurred while processing your request.", Details = appEx.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred during static JazzCash Get IBAN mock request.");
                return StatusCode(500, new { Error = "An unexpected server error occurred." });
            }
        }
    }
}