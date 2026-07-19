using ClassBooking.Domain.Common;

namespace ClassBooking.Domain.Users;

public sealed record AccountActivatedDomainEvent(Guid UserId, UserRole Role, DateTimeOffset OccurredAt) : IDomainEvent;
