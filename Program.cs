using Microsoft.AspNetCore.Mvc;
using SQLite;

var builder = WebApplication.CreateBuilder(args);

var dbConnectionString = builder.Configuration.GetSection("AppSettings").GetValue<string>("Database")
    ?? throw new NullReferenceException("Database not found");

var folderPath = builder.Configuration.GetSection("AppSettings").GetValue<string>("FolderPath")
    ?? throw new NullReferenceException("FolderPath not found");

var baseUrl = builder.Configuration.GetSection("AppSettings").GetValue<string>("BaseUrl")
    ?? throw new NullReferenceException("BaseUrl not found");

var db = new SQLiteConnection(new SQLiteConnectionString(dbConnectionString, true));
db.CreateTable<Entities.Image>();
db.CreateTable<Entities.Tag>();

var app = builder.Build();

app.MapGet("/", ([FromQuery] string? search) =>
{
    List<string> images;
    if (string.IsNullOrEmpty(search))
    {
        images = db.Table<Entities.Image>().Select(img => $"{baseUrl}/{img.Name}").ToList();
    }
    else
    {
        var searchTerms = search.ToLower().Trim().Split(',').ToList();
        var imageIds = db.Table<Entities.Tag>()
            .Where(t => searchTerms.Contains(t.Value))
            .Select(t => t.ImageId)
            .Distinct()
            .ToList();

        images = db.Table<Entities.Image>()
            .Where(img => imageIds.Contains(img.Id))
            .Select(img => $"{baseUrl}/{img.Name}")
            .ToList();
    }
    return Results.Ok(images);
});

app.MapPost("/", async (IFormFile file, [FromForm] string tags) =>
{
    const long maxFileSize = 5 * 1024 * 1024; // 5MB
    if (file.Length > maxFileSize)
    {
        return Results.BadRequest("File size exceeds 5MB limit.");
    }

    const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    var random = new Random();
    var randomName = new string([.. Enumerable.Repeat(chars, 7).Select(s => s[random.Next(s.Length)])]);

    var ext = Path.GetExtension(file.FileName);
    var fileName = randomName + ext;

    var filePath = Path.Combine(folderPath, fileName);
    using var stream = new FileStream(filePath, FileMode.Create);
    await file.CopyToAsync(stream);

    var cleanTags = tags.ToLower().Trim().Split(',').ToList();

    var img = new Entities.Image
    {
        Name = fileName,
    };

    db.Insert(img);

    db.InsertAll(cleanTags.Select(t => new Entities.Tag
    {
        ImageId = img.Id,
        Value = t
    }).ToList());

    return Results.Ok($"{baseUrl}/{fileName}");
}).DisableAntiforgery();

app.MapGet("/{filename}", (string filename) =>
{
    var filePath = Path.Combine(folderPath, filename);
    if (!File.Exists(filePath))
        return Results.NotFound();

    var ext = Path.GetExtension(filename).ToLowerInvariant();
    var contentType = ext switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".webp" => "image/webp",
        ".bmp" => "image/bmp",
        _ => "application/octet-stream"
    };
    return Results.File(Path.GetFullPath(filePath), contentType);
});

app.Run();

