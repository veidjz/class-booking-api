using ClassBooking.Domain.Common;

namespace ClassBooking.Domain.Users;

public class User : AggregateRoot
{
  protected User(
      Guid id,
      string name,
      string email,
      string passwordHash,
      UserRole role,
      DateTimeOffset createdAt)
      : base(id)
  {
    Name = name;
    Email = email;
    PasswordHash = passwordHash;
    Role = role;
    IsActive = true;
    CreatedAt = createdAt;
  }

  public string Name { get; private set; }

  public string Email { get; private set; }

  public string PasswordHash { get; private set; }

  public UserRole Role { get; }

  public bool IsActive { get; private set; }

  public DateTimeOffset CreatedAt { get; }

  public static User CreateAdmin(string name, string email, string passwordHash, DateTimeOffset createdAt) =>
      new User(Guid.CreateVersion7(createdAt), name, email, passwordHash, UserRole.Admin, createdAt);

  public bool Activate(DateTimeOffset occurredAt)
  {
    if (IsActive)
    {
      return false;
    }

    IsActive = true;
    Raise(new AccountActivatedDomainEvent(Id, Role, occurredAt));

    return true;
  }

  public bool Deactivate(DateTimeOffset occurredAt)
  {
    if (!IsActive)
    {
      return false;
    }

    IsActive = false;
    Raise(new AccountDeactivatedDomainEvent(Id, Role, occurredAt));

    return true;
  }

  public void RehashPassword(string passwordHash) => PasswordHash = passwordHash;
}
