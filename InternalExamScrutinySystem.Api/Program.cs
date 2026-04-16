using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using InternalExamScrutinySystem.Api.Data;
using InternalExamScrutinySystem.Api.Security;
using InternalExamScrutinySystem.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter your token in the text input below.\r\n\r\nNote: You do NOT need to type 'Bearer' anymore, just paste the token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
var sqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(sqlConnection);
});
// AutoMapper (profiles)
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// DI services
builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IModuleCoordinatorService, ModuleCoordinatorService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFacultyService, FacultyService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IHodService, HodService>();
builder.Services.AddScoped<IExamCoordinatorService, ExamCoordinatorService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IExamService, ExamService>();

// JWT Auth
var jwtSection = builder.Configuration.GetSection("Jwt");
var secret = jwtSection["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is missing.");
var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidIssuer = issuer,
            ValidAudience = audience,
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                // Detailed error for terminal debugging
                Console.WriteLine("JWT Auth Failed: " + context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("JWT Token Validated successfully.");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Initial Data Seeding
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
    
    // Apply any pending migrations safely without data loss
    db.Database.Migrate();

    var hod = db.Users.FirstOrDefault(u => u.Email == "hod@college.edu");
    if (hod == null)
    {
        hod = new AppUser
        {
            Name = "Dr. Krishna",
            Email = "hod@college.edu",
            RoleId = Role.HOD
        };
        hod.PasswordHash = hasher.HashPassword(hod, "Password123!");
        db.Users.Add(hod);
        Console.WriteLine("[SEED] Created default HOD user: hod@college.edu");
    }
    db.SaveChanges();

    db.SaveChanges();

    if (!db.Modules.Any())
    {
        var m1 = new Module { 
            ModuleCode = "CS101", 
            ModuleName = "Computer Science Fundamentals",
            Subjects = new List<ModuleSubject> { 
                new ModuleSubject { SubjectCode = "CSF1", SubjectName = "Programming in C" } 
            }
        };
        var m2 = new Module { 
            ModuleCode = "MAT201", 
            ModuleName = "Engineering Mathematics II",
            Subjects = new List<ModuleSubject> { 
                new ModuleSubject { SubjectCode = "EMA2", SubjectName = "Calculus & Algebra" } 
            }
        };
        db.Modules.AddRange(m1, m2);
        db.SaveChanges();
        Console.WriteLine("[SEED] Created default modules CS101 and MAT201.");
    }
    else if (!db.ModuleSubjects.Any())
    {
        // If modules exist but no subjects, add a sample subject to the first module
        var firstModule = db.Modules.First();
        firstModule.Subjects = new List<ModuleSubject> {
            new ModuleSubject { SubjectCode = "GEN101", SubjectName = "General Subject" },
            new ModuleSubject { SubjectCode = "ELECTIVE", SubjectName = "Sample Elective" }
        };
        db.SaveChanges();
        Console.WriteLine($"[SEED] Added sample subjects to module: {firstModule.ModuleName}");
    }
}

app.MapControllers();

app.Run();
