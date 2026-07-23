using ClassBooking.Domain.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace ClassBooking.Api.Auth;

internal static class AuthenticationExtensions
{
  internal static IServiceCollection AddApiAuthentication(this IServiceCollection services)
  {
    services.ConfigureOptions<JwtBearerConfiguration>();
    services.AddSingleton<IAuthorizationMiddlewareResultHandler, RoutingRejectionAuthorizationHandler>();
    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

    services.AddAuthorizationBuilder()
        .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
        .AddPolicy(AuthorizationPolicies.StudentOnly, policy =>
            policy.RequireClaim(AuthorizationPolicies.RoleClaim, nameof(UserRole.Student)))
        .AddPolicy(AuthorizationPolicies.TeacherOnly, policy =>
            policy.RequireClaim(AuthorizationPolicies.RoleClaim, nameof(UserRole.Teacher)))
        .AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
            policy.RequireClaim(AuthorizationPolicies.RoleClaim, nameof(UserRole.Admin)))
        .AddPolicy(AuthorizationPolicies.StudentOrAdmin, policy =>
            policy.RequireClaim(AuthorizationPolicies.RoleClaim, nameof(UserRole.Student), nameof(UserRole.Admin)));

    return services;
  }
}
