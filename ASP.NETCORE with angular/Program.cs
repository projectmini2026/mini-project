using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ASP.NETCORE_with_angular.data;
using ASP.NETCORE_with_angular.model;
using ASP.NETCORE_with_angular.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Application services
builder.Services.AddScoped<ProductRepository>();

// For hashing & verifying passwords
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();

// CORS so Angular can call API (adjust origin)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular",
        p => p
            .WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Enable Swagger only in Development by default
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.MapControllers();

app.Run();