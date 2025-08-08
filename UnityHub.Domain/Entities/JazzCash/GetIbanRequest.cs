// File: UnityHub.Domain/Entities/JazzCash/GetIbanRequest.cs
namespace UnityHub.Domain.Entities.JazzCash
{
    public class GetIbanRequest
    {
        public string Msisdn { get; set; } // This will be the dynamic input
        public string Channel { get; set; }
        public string RequestId { get; set; }
    }
}