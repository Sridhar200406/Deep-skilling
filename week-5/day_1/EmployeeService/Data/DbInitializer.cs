using EmployeeService.Data;

namespace EmployeeService.Data
{
    /// <summary>
    /// Ensures the Employee database is created and migrations are applied on startup.
    /// </summary>
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<EmployeeDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<EmployeeDbContext>>();

            try
            {
                await context.Database.EnsureCreatedAsync();
                logger.LogInformation("EmployeeService database initialized successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the EmployeeService database.");
            }
        }
    }
}
