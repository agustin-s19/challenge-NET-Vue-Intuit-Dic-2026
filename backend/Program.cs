using Microsoft.EntityFrameworkCore;
using ConsultasPlan.Data;
using ConsultasPlan.Middleware;
using ConsultasPlan.Services;
using ConsultasPlan.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// ===== Registro de servicios (antes de Build) =====
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=consultas.db"));

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IConsultaService, ConsultaService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddScoped<IAfiliadoService, AfiliadoService>();
var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();
app.Run();