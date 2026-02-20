using SQLite;

namespace Entities;

[Table("Tags")]
public class Tag
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    [Indexed]
    public int ImageId { get; set; }
    [Indexed]
    public string Value { get; set; } = null!;
}

