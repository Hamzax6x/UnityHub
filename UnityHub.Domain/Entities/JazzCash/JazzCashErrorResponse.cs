// File: UnityHub.Domain/Entities/JazzCash/JazzCashErrorResponse.cs
namespace UnityHub.Domain.Entities.JazzCash
{
    public class JazzCashErrorResponse
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        // Add any other common error details provided by JazzCash API
    }
}