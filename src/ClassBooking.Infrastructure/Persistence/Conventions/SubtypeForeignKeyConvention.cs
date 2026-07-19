using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace ClassBooking.Infrastructure.Persistence.Conventions;

internal sealed class SubtypeForeignKeyConvention : IModelFinalizingConvention
{
  public void ProcessModelFinalizing(
      IConventionModelBuilder modelBuilder,
      IConventionContext<IConventionModelBuilder> context)
  {
    foreach (IConventionEntityType entityType in modelBuilder.Metadata.GetEntityTypes())
    {
      foreach (IConventionForeignKey foreignKey in entityType.GetDeclaredForeignKeys())
      {
        if (foreignKey.PrincipalEntityType != entityType.BaseType)
        {
          continue;
        }

        string table = entityType.GetTableName()!;
        string principalTable = foreignKey.PrincipalEntityType.GetTableName()!;

        foreignKey.SetConstraintName($"fk_{table}_{principalTable}");
        foreignKey.SetDeleteBehavior(DeleteBehavior.Restrict);
      }
    }
  }
}
