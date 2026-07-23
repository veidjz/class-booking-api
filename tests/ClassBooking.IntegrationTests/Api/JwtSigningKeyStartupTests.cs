using ClassBooking.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace ClassBooking.IntegrationTests.Api;

public sealed class JwtSigningKeyStartupTests : IDisposable
{
  private readonly WebApplicationFactory<Program> _root = new WebApplicationFactory<Program>();

  public void Dispose() => _root.Dispose();

  [Fact]
  public void should_not_start_without_a_signing_key()
  {
    using WebApplicationFactory<Program> factory = CreateFactory(signingKey: null);

    Func<IServiceProvider> build = () => factory.Services;

    build.Should().Throw<OptionsValidationException>().WithMessage("*Jwt:SigningKey*");
  }

  [Fact]
  public void should_not_start_with_a_signing_key_shorter_than_thirty_two_bytes()
  {
    using WebApplicationFactory<Program> factory = CreateFactory(Convert.ToBase64String(new byte[31]));

    Func<IServiceProvider> build = () => factory.Services;

    build.Should().Throw<OptionsValidationException>().WithMessage("*Jwt:SigningKey*");
  }

  [Fact]
  public void should_not_start_with_a_signing_key_that_is_not_base64()
  {
    using WebApplicationFactory<Program> factory = CreateFactory("definitely-not-base64!!");

    Func<IServiceProvider> build = () => factory.Services;

    build.Should().Throw<OptionsValidationException>().WithMessage("*Jwt:SigningKey*");
  }

  [Fact]
  public void should_start_with_a_sixty_four_byte_signing_key()
  {
    using WebApplicationFactory<Program> factory = CreateFactory(Convert.ToBase64String(new byte[64]));

    Func<IServiceProvider> build = () => factory.Services;

    build.Should().NotThrow();
  }

  private WebApplicationFactory<Program> CreateFactory(string? signingKey) =>
      _root.WithWebHostBuilder(builder =>
      {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:Database", ApiHost.UnusedConnectionString);
        builder.UseSetting("RateLimiting:Enabled", "false");

        if (signingKey is not null)
        {
          builder.UseSetting("Jwt:SigningKey", signingKey);
        }
      });
}
