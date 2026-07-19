using ClassBooking.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassBooking.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
  public void Configure(EntityTypeBuilder<User> builder)
  {
    builder.ToTable("users");
    builder.HasKey(user => user.Id);
    builder.Ignore(user => user.DomainEvents);
    builder.Property(user => user.Role);
    builder.Property(user => user.CreatedAt);
  }
}
