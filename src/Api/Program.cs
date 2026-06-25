using Infrastructure.Hubs;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Domain.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<INotificationService, NotificationService>();

var app = builder.Build();

app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapPost("/api/documents/autosave", async (AppDbContext db, IHubContext<NotificationHub> hubContext, AutosaveRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest(new { message = "Text is required." });
    }

    var document = request.Id > 0
        ? await db.Documents.FindAsync(request.Id)
        : null;

    if (document == null)
    {
        document = new Domain.Entities.Document
        {
            Text = request.Text,
            Version = 1,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Documents.Add(document);
    }
    else
    {
        if (request.Version != document.Version)
        {
            return Results.Conflict(new
            {
                message = "Document version mismatch.",
                currentVersion = document.Version,
                currentText = document.Text,
            });
        }

        document.Text = request.Text;
        document.Version += 1;
        document.UpdatedAt = DateTime.UtcNow;
    }

    await db.SaveChangesAsync();

    // Broadcast the update via SignalR to all users viewing this document
    await hubContext.Clients.Group($"document-{document.ID}").SendAsync("ReceiveDocumentUpdate",
        new { documentId = document.ID, text = document.Text, version = document.Version });

    return Results.Ok(new AutosaveResponse(document.ID, document.Version));
});

app.MapGet("/api/documents/{id:int}", async (AppDbContext db, int id) =>
{
    var document = await db.Documents.FindAsync(id);
    return document is null
        ? Results.NotFound()
        : Results.Ok(new { document.ID, document.Text, document.Version, document.UpdatedAt });
});

app.MapGet("/api/documents", async (AppDbContext db) =>
{
    var documents = await db.Documents
        .OrderByDescending(d => d.UpdatedAt)
        .Select(d => new
        {
            d.ID,
            d.Version,
            d.UpdatedAt,
            snippet = d.Text.Length > 120 ? d.Text.Substring(0, 120) + "..." : d.Text
        })
        .ToListAsync();

    return Results.Ok(documents);
});

app.MapHub<NotificationHub>("/notificationHub");
app.Run();

record AutosaveRequest(int Id, string Text, int Version);
record AutosaveResponse(int Id, int Version);