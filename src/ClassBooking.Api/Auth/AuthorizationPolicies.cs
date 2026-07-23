namespace ClassBooking.Api.Auth;

internal static class AuthorizationPolicies
{
  internal const string RoleClaim = "role";

  internal const string StudentOnly = "StudentOnly";
  internal const string TeacherOnly = "TeacherOnly";
  internal const string AdminOnly = "AdminOnly";
  internal const string StudentOrAdmin = "StudentOrAdmin";
}
