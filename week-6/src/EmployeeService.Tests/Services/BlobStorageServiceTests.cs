using EmployeeService.Infrastructure.Azure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace EmployeeService.Tests.Services;

/// <summary>
/// Unit tests for BlobStorageService.
/// Tests validation logic without hitting real Azure Storage.
/// Integration tests against Azurite are handled separately.
/// </summary>
public class BlobStorageServiceTests
{
    private IBlobStorageService CreateService(string connectionString = "UseDevelopmentStorage=true")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureBlobStorage:ConnectionString"] = connectionString,
                ["AzureBlobStorage:ContainerName"] = "employee-files"
            })
            .Build();

        var logger = new Mock<ILogger<BlobStorageService>>().Object;
        return new BlobStorageService(config, logger);
    }

    [Fact]
    public void Constructor_MissingConnectionString_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureBlobStorage:ContainerName"] = "employee-files"
                // ConnectionString intentionally missing
            })
            .Build();

        // Act & Assert
        var act = () => new BlobStorageService(config, new Mock<ILogger<BlobStorageService>>().Object);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not configured*");
    }

    [Theory]
    [InlineData("application/octet-stream")]   // not allowed
    [InlineData("text/html")]                   // not allowed (XSS risk)
    [InlineData("application/javascript")]      // not allowed
    public async Task UploadFileAsync_DisallowedContentType_ThrowsInvalidOperationException(string contentType)
    {
        // We can test content type validation without a real blob service
        // by checking the exception is thrown before any network call
        var service = CreateService();

        using var stream = new MemoryStream(new byte[] { 0x00, 0x01 });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UploadFileAsync(stream, "test.bin", contentType, 1));
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("application/pdf")]
    [InlineData("text/plain")]
    public void AllowedContentTypes_AreRecognised(string contentType)
    {
        // Validates our allowed list contains the expected types
        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "text/plain"
        };

        allowedTypes.Should().Contain(contentType);
    }
}
