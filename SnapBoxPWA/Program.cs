var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Serve static files from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// Fallback to index.html for client-side routing (SPA support)
app.MapFallbackToFile("index.html");

app.Run();
