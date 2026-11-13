using Hlt.Domain.Common;

namespace Hlt.Domain.Organization;

/// <summary>Khoa / phòng khám – đơn vị của hàng đợi.</summary>
public class Department
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Location { get; private set; }

    private Department() { }
    public Department(string name, string? location = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tên khoa không được để trống.");
        Name = name.Trim();
        Location = location;
    }

    public void Update(string name, string? location)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tên khoa không được để trống.");
        Name = name.Trim();
        Location = location;
    }
}

/// <summary>Danh mục mức ưu tiên lâm sàng. LevelOrder nhỏ hơn được gọi trước.</summary>
public class PriorityLevel
{
    public int Id { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public short LevelOrder { get; private set; }
    public string? Description { get; private set; }

    public const string DefaultCode = "NORMAL";

    private PriorityLevel() { }
    public PriorityLevel(string code, string name, short levelOrder, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Mã mức ưu tiên không được để trống.");
        if (levelOrder < 1) throw new DomainException("Thứ tự ưu tiên phải lớn hơn 0.");
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        LevelOrder = levelOrder;
        Description = description;
    }

    public void Update(string name, short levelOrder, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Tên mức ưu tiên không được để trống.");
        if (levelOrder < 1) throw new DomainException("Thứ tự ưu tiên phải lớn hơn 0.");
        Name = name.Trim();
        LevelOrder = levelOrder;
        Description = description;
    }
}