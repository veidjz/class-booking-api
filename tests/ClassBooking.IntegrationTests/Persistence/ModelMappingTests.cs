using ClassBooking.Domain.Users;
using ClassBooking.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ClassBooking.IntegrationTests.Persistence;

public sealed class ModelMappingTests
{
  private static AppDbContext CreateContext()
  {
    DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
        .UseMySql(
            "Server=localhost;Database=classbooking;User Id=classbooking;Password=classbooking",
            new MySqlServerVersion(new Version(8, 4, 0)))
        .UseSnakeCaseNamingConvention()
        .Options;

    return new AppDbContext(options);
  }

  [Fact]
  public void should_map_version_as_a_shadow_concurrency_token_on_users()
  {
    using AppDbContext context = CreateContext();

    IProperty version = context.Model.FindEntityType(typeof(User))!.FindProperty("Version")!;

    version.Should().NotBeNull();
    version.IsShadowProperty().Should().BeTrue();
    version.IsConcurrencyToken.Should().BeTrue();
    version.GetColumnName().Should().Be("version");
  }

  [Fact]
  public void should_not_expose_a_version_member_on_the_domain_type()
  {
    typeof(User).GetProperties().Should().NotContain(property => property.Name == "Version");
  }
}
