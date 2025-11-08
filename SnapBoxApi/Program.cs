using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OpenAI;
using OpenAI.Chat;
using SnapBoxApi.Middleware;
using SnapBoxApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FormOptions>(options => { options.MultipartBodyLengthLimit = 104857600; });

// Add CORS services with a more explicit policy
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("*"); // Expose all response headers
    });
});

builder.Services.AddControllers();
builder.Services.AddSingleton<BlobServiceClient>(sp =>
    new BlobServiceClient(new Uri(builder.Configuration.GetConnectionString("AzureBlobConnection"))));
builder.Services.AddSingleton<OpenAIClient>(sp =>
    new OpenAIClient(builder.Configuration["OpenAI:ApiKey"]));
builder.Services.AddSingleton<CosmosDbService>();
builder.Services.AddScoped<ImageDescriptionService>();
builder.Services.AddScoped<DataService>();

var app = builder.Build();

// Enable CORS before HTTPS redirection and other middleware
// This ensures CORS headers are added to all responses, including errors
app.UseCors();

app.UseHttpsRedirection();

app.UseMiddleware<BasicAuthMiddleware>();
app.MapControllers();

app.Run();

