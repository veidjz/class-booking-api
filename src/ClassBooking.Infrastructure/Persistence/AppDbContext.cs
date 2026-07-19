using ClassBooking.Application.Abstractions.Data;
using ClassBooking.Domain.Users;
using ClassBooking.Infrastructure.Persistence.Conventions;
using ClassBooking.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClassBooking.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
  public DbSet<User> Users => Set<User>();

  protected override void OnModelCreating(ModelBuilder modelBuilder) =>
      modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
  {
    configurationBuilder.Conventions.Add(_ => new SubtypeForeignKeyConvention());

    configurationBuilder.Properties<Guid>()
        .HaveConversion<GuidToBinary16Converter, GuidToBinary16Comparer>()
        .HaveColumnType("binary(16)");

    configurationBuilder.Properties<DateTimeOffset>()
        .HaveConversion<UtcDateTimeOffsetConverter>()
        .HaveColumnType("datetime(6)");

    configurationBuilder.Properties<UserRole>()
        .HaveConversion<EnumToStringConverter<UserRole>>()
        .HaveMaxLength(30);
  }
}
