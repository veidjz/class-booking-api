using ClassBooking.Application.Abstractions.Data;
using ClassBooking.Domain.Users;
using ClassBooking.Infrastructure.Persistence;
using ClassBooking.Infrastructure.Persistence.Interceptors;
using ClassBooking.Infrastructure.Time;
using ClassBooking.IntegrationTests.Persistence.Fixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ClassBooking.IntegrationTests.Persistence;

[Collection(nameof(DatabaseCollection))]
public sealed class HostCompositionTests(ContainersFixture fixture) : DatabaseTestBase(fixture)
{
  [Fact]
  public void should_resolve_the_persistence_graph()
  {
    using WebApplicationFactory<Program> factory = CreateFactory();
    using IServiceScope scope = factory.Services.CreateScope();

    AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    IAppDbContext appDbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
    IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    IUserRepository repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

    appDbContext.Should().BeSameAs(context);
    unitOfWork.Should().NotBeNull();
    repository.Should().NotBeNull();
  }

  [Fact]
  public void should_share_a_single_version_token_interceptor()
  {
    using WebApplicationFactory<Program> factory = CreateFactory();

    VersionTokenInterceptor first = factory.Services.GetRequiredService<VersionTokenInterceptor>();
    VersionTokenInterceptor second = factory.Services.GetRequiredService<VersionTokenInterceptor>();

    first.Should().BeSameAs(second);
  }

  [Fact]
  public void should_register_the_microsecond_clock()
  {
    using WebApplicationFactory<Program> factory = CreateFactory();

    TimeProvider timeProvider = factory.Services.GetRequiredService<TimeProvider>();

    timeProvider.Should().BeOfType<MicrosecondTimeProvider>();
  }

  private WebApplicationFactory<Program> CreateFactory() =>
      new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
          builder.UseSetting("ConnectionStrings:Database", Fixture.ConnectionString));
}
