using ClassBooking.Domain.Common;

namespace ClassBooking.Domain.Users;

public sealed record TeacherAccountCreatedDomainEvent(Guid TeacherId, DateTimeOffset OccurredAt) : IDomainEvent;
