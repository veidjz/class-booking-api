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
    builder.Property(user => user.Name).HasMaxLength(120).IsRequired();
    builder.Property(user => user.Email).HasMaxLength(254).IsRequired();
    builder.Property(user => user.PasswordHash).HasMaxLength(100).IsRequired();
    builder.Property(user => user.Role);
    builder.Property(user => user.IsActive).HasDefaultValue(true).HasSentinel(true);
    builder.Property(user => user.CreatedAt);
    builder.Property<int>("Version").IsConcurrencyToken().HasDefaultValue(0).HasSentinel(-1);
  }
}
