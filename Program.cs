var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapPost("/images", async (IFormFile image) =>
{
    const long maxFileSize = 5 * 1024 * 1024; // 5MB
    if (image.Length > maxFileSize)
    {
        return Results.BadRequest("File size exceeds 5MB limit.");
    }

    const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    var random = new Random();
    var randomName = new string([.. Enumerable.Repeat(chars, 7).Select(s => s[random.Next(s.Length)])]);

    var ext = Path.GetExtension(image.FileName);
    var fileName = randomName + ext;

    var filePath = Path.Combine("uploads", fileName);
    using var stream = new FileStream(filePath, FileMode.Create);
    await image.CopyToAsync(stream);

    return Results.Ok();
}).DisableAntiforgery();

app.MapGet("/images", () =>
{
    var images = Directory
        .GetFiles("uploads")
        .Select(file => Path.GetFileName(file));
    return Results.Ok(images);
});

app.MapGet("/images/{filename}", (string filename) =>
{
    var filePath = Path.Combine("uploads", filename);
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
