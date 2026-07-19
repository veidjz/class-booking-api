using ClassBooking.Domain.Common;

namespace ClassBooking.Domain.Users;

public sealed record AccountDeactivatedDomainEvent(Guid UserId, UserRole Role, DateTimeOffset OccurredAt) : IDomainEvent;
