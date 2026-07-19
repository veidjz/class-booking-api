using ClassBooking.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassBooking.Infrastructure.Persistence.Configurations;

internal sealed class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
  public void Configure(EntityTypeBuilder<Teacher> builder) => builder.ToTable("teachers");
}
