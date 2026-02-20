using SQLite;

namespace Entities;

[Table("Images")]
public class Image
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

