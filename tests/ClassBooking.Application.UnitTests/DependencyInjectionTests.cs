using ClassBooking.Application.Abstractions.Data;
using ClassBooking.Application.Abstractions.Messaging;
using ClassBooking.Application.Behaviors;
using ClassBooking.Domain.Common;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace ClassBooking.Application.UnitTests;

public sealed class DependencyInjectionTests
{
  private sealed record PingCommand : ICommand;

  private sealed record PingQuery : IQuery<int>;

  private static ServiceProvider BuildProvider()
  {
    ServiceCollection services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton(Substitute.For<IUnitOfWork>());
    services.AddApplication();
    return services.BuildServiceProvider();
  }

  [Fact]
  public void should_register_behaviors_in_pipeline_order_when_application_added()
  {
    using ServiceProvider provider = BuildProvider();

    Type[] behaviors = provider.GetServices<IPipelineBehavior<PingCommand, Result>>()
        .Select(behavior => behavior.GetType().GetGenericTypeDefinition())
        .ToArray();

    behaviors.Should().Equal(
        typeof(RequestLoggingBehavior<,>),
        typeof(ValidationBehavior<,>),
        typeof(UnitOfWorkBehavior<,>));
  }

  [Fact]
  public void should_not_apply_unit_of_work_behavior_to_queries()
  {
    using ServiceProvider provider = BuildProvider();

    Type[] behaviors = provider.GetServices<IPipelineBehavior<PingQuery, Result<int>>>()
        .Select(behavior => behavior.GetType().GetGenericTypeDefinition())
        .ToArray();

    behaviors.Should().Equal(
        typeof(RequestLoggingBehavior<,>),
        typeof(ValidationBehavior<,>));
  }

  [Fact]
  public void should_register_system_time_provider_when_none_registered()
  {
    using ServiceProvider provider = BuildProvider();

    provider.GetRequiredService<TimeProvider>().Should().BeSameAs(TimeProvider.System);
  }

  [Fact]
  public void should_keep_existing_time_provider_when_already_registered()
  {
    ServiceCollection services = new ServiceCollection();
    TimeProvider existing = Substitute.ForPartsOf<TimeProvider>();
    services.AddSingleton(existing);
    services.AddApplication();
    using ServiceProvider provider = services.BuildServiceProvider();

    provider.GetRequiredService<TimeProvider>().Should().BeSameAs(existing);
  }
}
