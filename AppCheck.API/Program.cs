using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using AppCheck.Data;
using AppCheck.API.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppCheckDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configurar JWT
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// 3. Registrar servicios
builder.Services.AddSingleton<JwtService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 4. Swagger con seguridad JWT (esto agrega el candado)
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "AppCheck API", Version = "v1" });

    // Definir el esquema de seguridad Bearer
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingresa el token JWT. Ejemplo: 'eyJhbGci...' (sin la palabra 'Bearer', Swagger la agrega automáticamente)"
    });

    // Aplicar el esquema globalmente
    // Aplicar el esquema globalmente (sintaxis .NET 10 + Swashbuckle v10)
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

// 5. Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 6. Endpoint de health
app.MapGet("/api/health", async (AppCheckDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();
        if (canConnect)
        {
            return Results.Ok(new
            {
                Status = "AppCheck API funcionando",
                Version = "1.0.0",
                Database = "Conectada a AppcheckAPI",
                Timestamp = DateTimeOffset.UtcNow
            });
        }
        return Results.Problem("No se pudo conectar a la base de datos.");
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error de conexión: {ex.Message}");
    }
});

app.Run();
