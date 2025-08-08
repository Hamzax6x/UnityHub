// File: UnityHub.Domain/Entities/JazzCash/GetIbanResponse.cs
using Newtonsoft.Json; // REQUIRED for JsonProperty

namespace UnityHub.Domain.Entities.JazzCash
{
    public class GetIbanResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("response")]
        public JazzCashApiResponseDetail ResponseDetail { get; set; }

        [JsonProperty("MSISDN")]
        public string Msisdn { get; set; }

        [JsonProperty("IBAN")]
        public string Iban { get; set; }

        [JsonProperty("title")]
        public string AccountTitle { get; set; }

        [JsonProperty("bankIMD")]
        public string BankImd { get; set; }
    }

    public class JazzCashApiResponseDetail
    {
        [JsonProperty("responseCode")]
        public string ResponseCode { get; set; }

        [JsonProperty("responseDescription")]
        public string ResponseDescription { get; set; }
    }
}