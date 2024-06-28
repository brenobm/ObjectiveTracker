using Microsoft.Azure.Cosmos;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.WithOrigins("https://localhost:7128", "http://localhost:5161")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddControllers();

builder.Services.AddSingleton<CosmosClient>(serviceProvider => new CosmosClient(
    builder.Configuration["CosmosDb:AccountEndpoint"],
    builder.Configuration["CosmosDb:AccountKey"]
));

builder.Services.AddSingleton<Container>(serviceProvider =>
{
    var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();
    return cosmosClient.GetContainer(
        builder.Configuration["CosmosDb:DatabaseName"],
        builder.Configuration["CosmosDb:ContainerName"]
    );
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Objectives Tracking API", Version = "v1" });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
