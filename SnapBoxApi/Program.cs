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
builder.Services.AddControllers();
builder.Services.AddSingleton<BlobServiceClient>(sp =>
    new BlobServiceClient(new Uri(builder.Configuration.GetConnectionString("AzureBlobConnection"))));
builder.Services.AddSingleton<OpenAIClient>(sp =>
    new OpenAIClient(builder.Configuration["OpenAI:ApiKey"]));
builder.Services.AddSingleton<CosmosDbService>();
builder.Services.AddScoped<ImageDescriptionService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseMiddleware<BasicAuthMiddleware>();
app.MapControllers();

app.Run();

