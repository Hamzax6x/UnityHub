// File: UnityHub.Infrastructure/Services/JazzCashRaastService.cs
using System;
using System.Threading.Tasks;
using UnityHub.Domain.Entities.JazzCash;
using UnityHub.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace UnityHub.Infrastructure.Services
{
    public class JazzCashRaastService : IJazzCashRaastService
    {
        private readonly ILogger<JazzCashRaastService> _logger;

        public JazzCashRaastService(ILogger<JazzCashRaastService> logger)
        {
            _logger = logger;
        }

        public Task<GetIbanResponse> GetIbanAsync(GetIbanRequest request) // Now accepts the request object
        {
            _logger.LogInformation("JazzCashRaastService is returning STATIC IBAN data based on input MSISDN.");

            // Basic validation for the input request
            if (request == null || string.IsNullOrWhiteSpace(request.Msisdn))
            {
                _logger.LogWarning("Invalid GetIbanRequest received. MSISDN is required.");
                // You might return a specific error response here or throw an exception
                return Task.FromResult(new GetIbanResponse
                {
                    Success = false,
                    RequestId = request?.RequestId ?? "N/A",
                    ResponseDetail = new JazzCashApiResponseDetail
                    {
                        ResponseCode = "01", // Example error code for validation failure
                        ResponseDescription = "Invalid MSISDN provided"
                    }
                });
            }

            // --- Define your STATIC Response Data, utilizing the input MSISDN ---
            // The other fields (IBAN, AccountTitle, BankImd) are still "given by your code" (hardcoded here)
            // but the MSISDN from the request is used.
            var staticResponse = new GetIbanResponse
            {
                Success = true,
                RequestId = request.RequestId, // Use the RequestId from the input request
                ResponseDetail = new JazzCashApiResponseDetail
                {
                    ResponseCode = "00",
                    ResponseDescription = "OK"
                },
                Msisdn = request.Msisdn, // <<< Use the MSISDN from the input request
                Iban = "PK11JCMA2607923214675202", // This IBAN is "given by your code" statically
                AccountTitle = "MUHAMMAD HAMZA QURESHI", // This is "given by your code" statically
                BankImd = "WMBLPKKA" // This is "given by your code" statically
            };
            // --- End Static Response Data ---

            _logger.LogInformation("Static JazzCash GET IBAN data prepared successfully for MSISDN: {Msisdn}. IBAN: {Iban}, Title: {Title}", staticResponse.Msisdn, staticResponse.Iban, staticResponse.AccountTitle);

            return Task.FromResult(staticResponse);
        }
    }
}