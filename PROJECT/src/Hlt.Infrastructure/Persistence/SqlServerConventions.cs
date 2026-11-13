using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hlt.Infrastructure.Persistence;

/// <summary>
/// Quy ước áp dụng cho toàn bộ mô hình:
///  - Tên cột, khóa, chỉ mục dạng snake_case (đúng quy ước ở mục 4.4 của báo cáo).
///  - Enum lưu dạng chữ HOA (PENDING, CHECKED_IN...) để đọc được khi xem bảng.
///  - Mọi khóa ngoại dùng ON DELETE NO ACTION: SQL Server báo lỗi
///    "may cause cycles or multiple cascade paths" nếu để xóa dây chuyền.
/// </summary>
internal static class SqlServerConventions
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var p in entity.GetProperties())
            {
                p.SetColumnName(ToSnake(p.GetColumnName()!));

                var type = Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType;
                if (type.IsEnum)
                {
                    var converter = (ValueConverter)Activator.CreateInstance(typeof(EnumTextConverter<>).MakeGenericType(type))!;
                    p.SetValueConverter(converter);
                    p.SetMaxLength(30);
                    p.SetIsUnicode(false); // varchar
                }
            }

            foreach (var key in entity.GetKeys())
                key.SetName(ToSnake(key.GetName()!));

            foreach (var fk in entity.GetForeignKeys())
            {
                fk.SetConstraintName(ToSnake(fk.GetConstraintName()!));
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }

            foreach (var index in entity.GetIndexes())
                index.SetDatabaseName(ToSnake(index.GetDatabaseName()!));
        }
    }

    /// <summary>"OidcSubject" -> "oidc_subject", "PK_app_user" -> "pk_app_user".</summary>
    public static string ToSnake(string name)
    {
        var sb = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                var prev = i > 0 ? name[i - 1] : '_';
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                if (prev != '_' && (char.IsLower(prev) || char.IsDigit(prev) || (char.IsUpper(prev) && nextIsLower)))
                    sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }
}

/// <summary>Chuyển enum &lt;-&gt; chuỗi: CheckedIn &lt;-&gt; "CHECKED_IN".</summary>
public class EnumTextConverter<TEnum>() : ValueConverter<TEnum, string>(
    v => EnumText.ToDb(v),
    s => EnumText.FromDb<TEnum>(s))
    where TEnum : struct, Enum
{
}

public static class EnumText
{
    public static string ToDb<TEnum>(TEnum value) where TEnum : struct, Enum
        => SqlServerConventions.ToSnake(value.ToString()).ToUpperInvariant();

    public static TEnum FromDb<TEnum>(string value) where TEnum : struct, Enum
        => Enum.Parse<TEnum>(value.Replace("_", ""), ignoreCase: true);
}