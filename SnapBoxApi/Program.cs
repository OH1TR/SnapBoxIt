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

// Add CORS services
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
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

app.UseHttpsRedirection();

// Enable CORS before authentication middleware
app.UseCors();

app.UseMiddleware<BasicAuthMiddleware>();
app.MapControllers();

app.Run();

