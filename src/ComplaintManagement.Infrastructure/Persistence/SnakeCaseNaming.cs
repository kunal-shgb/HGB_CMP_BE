using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ComplaintManagement.Infrastructure.Persistence;

/// <summary>Applies PostgreSQL snake_case names to tables, columns, keys and indexes.</summary>
internal static class SnakeCaseNaming
{
    public static void UseSnakeCaseNames(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var table = entity.GetTableName();
            if (table is not null && entity.GetViewName() is null) entity.SetTableName(ToSnake(table));

            var storeObject = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
            foreach (var property in entity.GetProperties())
                property.SetColumnName(ToSnake(property.GetColumnName(storeObject) ?? property.Name));
            foreach (var key in entity.GetKeys())
                key.SetName(ToSnake(key.GetName()!));
            foreach (var fk in entity.GetForeignKeys())
                fk.SetConstraintName(ToSnake(fk.GetConstraintName()!));
            foreach (var index in entity.GetIndexes())
                index.SetDatabaseName(ToSnake(index.GetDatabaseName()!));
        }
    }

    internal static string ToSnake(string name)
    {
        var sb = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                var prevIsLowerOrDigit = i > 0 && (char.IsLower(name[i - 1]) || char.IsDigit(name[i - 1]));
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                var prevIsUpper = i > 0 && char.IsUpper(name[i - 1]);
                if (i > 0 && name[i - 1] != '_' && (prevIsLowerOrDigit || (prevIsUpper && nextIsLower))) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }
}
