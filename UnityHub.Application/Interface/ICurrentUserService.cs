namespace UnityHub.Application.Interfaces.Services
{
    public interface ICurrentUserService
    {
        long UserId { get; }
        string ClientIpAddress { get; }      // ✅ NEW
        string BrowserInfo { get; }
    }
}
