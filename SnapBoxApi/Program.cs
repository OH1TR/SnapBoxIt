using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OpenAI;
using OpenAI.Chat;
using SnapBoxApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Lis‰‰ palvelut
builder.Services.AddControllers();
builder.Services.AddSingleton<BlobServiceClient>(sp =>
    new BlobServiceClient(builder.Configuration.GetConnectionString("AzureBlobConnection")));
builder.Services.AddSingleton<OpenAIClient>(sp =>
    new OpenAIClient(builder.Configuration["OpenAI:ApiKey"]));
builder.Services.AddSingleton<CosmosDbService>();
builder.Services.AddScoped<ImageDescriptionService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

