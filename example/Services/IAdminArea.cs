using BlackDigital.Rest;

namespace BlackDigital.AspNet.Example.Services
{
    [Service("api/admin")]
    public interface IAdminArea
    {
        [Action("dashboard", method: RestMethod.Get)]
        Task<string> GetDashboardAsync();

        [Action("reports", method: RestMethod.Get)]
        Task<string> GetReportsAsync();
    }
}
