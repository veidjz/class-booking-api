namespace ClassBooking.Domain.Common;

public abstract class Entity(Guid id) : IEquatable<Entity>
{
  public Guid Id { get; } = id;

  public bool Equals(Entity? other) =>
      other is not null && other.GetType() == GetType() && other.Id == Id;

  public override bool Equals(object? obj) => Equals(obj as Entity);

  public override int GetHashCode() => HashCode.Combine(GetType(), Id);

  public static bool operator ==(Entity? left, Entity? right) =>
      left is null ? right is null : left.Equals(right);

  public static bool operator !=(Entity? left, Entity? right) => !(left == right);
}
