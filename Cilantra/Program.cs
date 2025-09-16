using Microsoft.EntityFrameworkCore;
using Cilantra.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddDbContext<CilantraDbContext>(options => options.UseSqlite("Data Source=cilantra.db"));
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();
app.MapControllers();
app.Run();
