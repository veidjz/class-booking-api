using ClassBooking.Domain.Common;

namespace ClassBooking.Domain.Users;

public sealed record StudentRegisteredDomainEvent(Guid StudentId, DateTimeOffset OccurredAt) : IDomainEvent;
