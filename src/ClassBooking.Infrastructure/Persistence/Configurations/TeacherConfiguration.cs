using ClassBooking.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClassBooking.Infrastructure.Persistence.Configurations;

internal sealed class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
  public void Configure(EntityTypeBuilder<Teacher> builder)
  {
    builder.ToTable("teachers", table => table.HasCheckConstraint(
        "chk_teachers_counters",
        "cancellation_count >= 0 AND late_cancellation_count >= 0 AND no_show_count >= 0"));
    builder.Property(teacher => teacher.CancellationCount).HasDefaultValue(0);
    builder.Property(teacher => teacher.LateCancellationCount).HasDefaultValue(0);
    builder.Property(teacher => teacher.NoShowCount).HasDefaultValue(0);
  }
}
