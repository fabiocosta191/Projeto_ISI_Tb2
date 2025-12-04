using Microsoft.EntityFrameworkCore;
using SafeHome.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Necessário para o Swagger
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Adicionar serviços ao contentor
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- CONFIGURAÇÃO DO SWAGGER COM SUPORTE A JWT ---
// (Tem de ser ANTES do app.Build)
// Configuração do Swagger para não precisares de escrever "Bearer"
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http, // MUDANÇA AQUI: Usar Http em vez de ApiKey
        Scheme = "bearer", // O Swagger vai adicionar "Bearer " automaticamente
        BearerFormat = "JWT",
        Description = "Insira apenas o token JWT (sem escrever Bearer)"
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
            new List<string>()
        }
    });
});
// --------------------------------------------------

// Ligar à Base de Dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- CONFIGURAÇÃO JWT ---
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
// ------------------------

// 2. CONSTRUIR A APLICAÇÃO (A Fronteira!)
var app = builder.Build();

// 3. Configurar o Pipeline (Comportamento)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Isto mostra a página azul!
}

app.UseHttpsRedirection();

app.UseAuthentication(); // 1º Valida quem é (Crachá)
app.UseAuthorization();  // 2º Valida o que pode fazer (Acesso)

app.MapControllers();

app.Run();