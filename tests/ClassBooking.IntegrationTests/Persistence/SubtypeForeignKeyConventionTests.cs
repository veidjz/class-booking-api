using ClassBooking.Infrastructure.Persistence.Conventions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ClassBooking.IntegrationTests.Persistence;

public sealed class SubtypeForeignKeyConventionTests
{
  [Fact]
  public void should_name_and_restrict_the_link_to_the_base_table()
  {
    using ConventionProbeContext context = CreateContext();

    IForeignKey link = FindForeignKey(context, nameof(ConventionSubtype.Id));

    link.GetConstraintName().Should().Be("fk_subtypes_roots");
    link.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
  }

  [Fact]
  public void should_leave_business_relationships_to_the_base_table_untouched()
  {
    using ConventionProbeContext context = CreateContext();

    IForeignKey business = FindForeignKey(context, nameof(ConventionSubtype.ApprovedById));

    business.GetConstraintName().Should().NotBe("fk_subtypes_roots");
    business.DeleteBehavior.Should().Be(DeleteBehavior.SetNull);
  }

  private static ConventionProbeContext CreateContext()
  {
    DbContextOptions<ConventionProbeContext> options = new DbContextOptionsBuilder<ConventionProbeContext>()
        .UseMySql(
            "Server=localhost;Database=classbooking;User Id=classbooking;Password=classbooking",
            new MySqlServerVersion(new Version(8, 4, 0)))
        .UseSnakeCaseNamingConvention()
        .Options;

    return new ConventionProbeContext(options);
  }

  private static IForeignKey FindForeignKey(ConventionProbeContext context, string propertyName) =>
      context.Model.FindEntityType(typeof(ConventionSubtype))!
          .GetDeclaredForeignKeys()
          .Single(foreignKey => foreignKey.Properties.Single().Name == propertyName);
}

public class ConventionRoot
{
  public Guid Id { get; set; }

  public string Name { get; set; } = string.Empty;
}

public sealed class ConventionSubtype : ConventionRoot
{
  public Guid? ApprovedById { get; set; }

  public ConventionRoot? ApprovedBy { get; set; }
}

public sealed class ConventionProbeContext(DbContextOptions<ConventionProbeContext> options) : DbContext(options)
{
  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<ConventionRoot>().ToTable("roots");
    modelBuilder.Entity<ConventionSubtype>().ToTable("subtypes");
    modelBuilder.Entity<ConventionSubtype>()
        .HasOne(subtype => subtype.ApprovedBy)
        .WithMany()
        .HasForeignKey(subtype => subtype.ApprovedById)
        .OnDelete(DeleteBehavior.SetNull);
  }

  protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
      configurationBuilder.Conventions.Add(_ => new SubtypeForeignKeyConvention());
}
