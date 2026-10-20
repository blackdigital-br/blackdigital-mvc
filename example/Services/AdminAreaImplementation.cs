using BlackDigital.Domain.Security;

namespace BlackDigital.AspNet.Example.Services
{
    // Class-level requirement: it applies to every action of this service.
    [Authorize(Roles = new[] { "Admin" })]
    public class AdminAreaImplementation : IAdminArea
    {
        public Task<string> GetDashboardAsync()
            => Task.FromResult("Class-level [Authorize(Roles = Admin)] applies to every action.");

        // Combined with the class-level requirement using OR semantics, so the
        // caller may be either 'Admin' (from the class) or 'Manager' (from here).
        [Authorize(Roles = new[] { "Manager" })]
        public Task<string> GetReportsAsync()
            => Task.FromResult("Admin (class-level) OR Manager (method-level).");
    }
}
