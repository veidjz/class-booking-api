using ClassBooking.Application.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ClassBooking.Application;

public static class DependencyInjection
{
  public static IServiceCollection AddApplication(this IServiceCollection services)
  {
    services.AddMediatR(configuration =>
    {
      configuration.RegisterServicesFromAssembly(ApplicationAssemblyReference.Assembly);
      configuration.AddOpenBehavior(typeof(RequestLoggingBehavior<,>));
      configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
      configuration.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
    });

    services.AddValidatorsFromAssembly(ApplicationAssemblyReference.Assembly, includeInternalTypes: true);

    services.TryAddSingleton(TimeProvider.System);

    return services;
  }
}
