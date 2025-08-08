// File: UnityHub.Infrastructure/Interfaces/IJazzCashRaastService.cs
using UnityHub.Domain.Entities.JazzCash;
using System.Threading.Tasks;

namespace UnityHub.Infrastructure.Interfaces
{
    public interface IJazzCashRaastService
    {
        // Now accepts GetIbanRequest
        Task<GetIbanResponse> GetIbanAsync(GetIbanRequest request);
    }
}