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
public sealed class HostCompositionTests : DatabaseTestBase, IDisposable
{
  private readonly WebApplicationFactory<Program> _root = new WebApplicationFactory<Program>();
  private readonly WebApplicationFactory<Program> _factory;

  public HostCompositionTests(ContainersFixture fixture)
      : base(fixture) =>
      _factory = _root.WithWebHostBuilder(builder =>
          builder.UseSetting("ConnectionStrings:Database", fixture.ConnectionString));

  public void Dispose() => _root.Dispose();

  [Fact]
  public void should_resolve_the_persistence_graph()
  {
    using IServiceScope scope = _factory.Services.CreateScope();

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
    VersionTokenInterceptor first = _factory.Services.GetRequiredService<VersionTokenInterceptor>();
    VersionTokenInterceptor second = _factory.Services.GetRequiredService<VersionTokenInterceptor>();

    first.Should().BeSameAs(second);
  }

  [Fact]
  public void should_register_the_microsecond_clock()
  {
    TimeProvider timeProvider = _factory.Services.GetRequiredService<TimeProvider>();

    timeProvider.Should().BeOfType<MicrosecondTimeProvider>();
  }
}
