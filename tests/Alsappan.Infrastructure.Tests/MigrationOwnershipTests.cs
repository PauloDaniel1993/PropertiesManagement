using System.Reflection;
using Alsappan.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Alsappan.Infrastructure.Tests;

public sealed class MigrationOwnershipTests
{
  [Fact]
  public void OccurrenceModuleDoesNotCreateOrDropPetOwnedTables()
  {
    var upTables = BuildOperations(new OccurrenceModule(), "Up")
      .OfType<CreateTableOperation>()
      .Select(operation => operation.Name)
      .ToArray();
    var downTables = BuildOperations(new OccurrenceModule(), "Down")
      .OfType<DropTableOperation>()
      .Select(operation => operation.Name)
      .ToArray();

    Assert.DoesNotContain("pets", upTables);
    Assert.DoesNotContain("pet_document_links", upTables);
    Assert.DoesNotContain("pets", downTables);
    Assert.DoesNotContain("pet_document_links", downTables);
  }

  private static List<MigrationOperation> BuildOperations(Migration migration, string methodName)
  {
    var method = migration.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
    Assert.NotNull(method);

    var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
    method.Invoke(migration, [builder]);
    return builder.Operations;
  }
}
