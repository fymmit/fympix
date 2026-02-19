using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

var folderPath = builder.Configuration.GetSection("AppSettings").GetValue<string>("FolderPath")
    ?? throw new NullReferenceException("FolderPath not found");

var baseUrl = builder.Configuration.GetSection("AppSettings").GetValue<string>("BaseUrl")
    ?? throw new NullReferenceException("BaseUrl not found");

var app = builder.Build();

app.MapGet("/", () =>
{
    var images = Directory
        .GetFiles(folderPath)
        .Select(file => Path.GetFileName(file));
    return Results.Ok(images);
});

app.MapPost("/", async (IFormFile image) =>
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

    var filePath = Path.Combine(folderPath, fileName);
    using var stream = new FileStream(filePath, FileMode.Create);
    await image.CopyToAsync(stream);

    return Results.Json(new { Url = $"{baseUrl}/{fileName}" });
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
