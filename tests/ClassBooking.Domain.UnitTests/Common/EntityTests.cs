using ClassBooking.Domain.Common;
using FluentAssertions;

namespace ClassBooking.Domain.UnitTests.Common;

public sealed class EntityTests
{
  private sealed class FirstEntity(Guid id) : Entity(id);

  private sealed class SecondEntity(Guid id) : Entity(id);

  [Fact]
  public void should_expose_id()
  {
    Guid id = Guid.CreateVersion7();

    FirstEntity entity = new FirstEntity(id);

    entity.Id.Should().Be(id);
  }

  [Fact]
  public void should_be_equal_when_same_type_and_id()
  {
    Guid id = Guid.CreateVersion7();

    FirstEntity first = new FirstEntity(id);
    FirstEntity second = new FirstEntity(id);

    first.Equals(second).Should().BeTrue();
    (first == second).Should().BeTrue();
    first.GetHashCode().Should().Be(second.GetHashCode());
  }

  [Fact]
  public void should_not_be_equal_when_ids_differ()
  {
    FirstEntity first = new FirstEntity(Guid.CreateVersion7());
    FirstEntity second = new FirstEntity(Guid.CreateVersion7());

    first.Equals(second).Should().BeFalse();
    (first != second).Should().BeTrue();
  }

  [Fact]
  public void should_not_be_equal_when_types_differ()
  {
    Guid id = Guid.CreateVersion7();

    FirstEntity first = new FirstEntity(id);
    SecondEntity second = new SecondEntity(id);

    first.Equals(second).Should().BeFalse();
  }

  [Fact]
  public void should_not_be_equal_when_other_is_null()
  {
    FirstEntity? entity = new FirstEntity(Guid.CreateVersion7());

    entity.Equals(null).Should().BeFalse();
    (entity == null).Should().BeFalse();
    ((FirstEntity?)null == null).Should().BeTrue();
  }
}
