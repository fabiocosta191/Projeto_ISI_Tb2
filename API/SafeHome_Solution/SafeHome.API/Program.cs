using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SafeHome.API.Options;
using SafeHome.API.Services;
using SafeHome.API.Soap;
using SafeHome.Data;
using SoapCore;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. REGISTO DE SERVIÇOS (DI Container)
// ==========================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// --- CORS (para permitir front-end externo) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        // Para desenvolvimento: permitir tudo (inclui file://, localhost, etc.)
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
        // Em produção, podes trocar para:
        // .WithOrigins("https://o-teu-front-end.com")
    });
});

// --- Swagger (Com suporte a JWT Bearer) ---
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
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
                    Id = "Bearer",
                }
            },
            new List<string>()
        }
    });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

// --- Base de Dados ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// --- Autenticação JWT ---
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

// --- Serviço SOAP (SoapCore) ---
builder.Services.AddSoapCore();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddScoped<ISensorReadingService, SensorReadingService>();
builder.Services.AddScoped<IUserService, UserService>();

// --- Serviços Externos (HttpClient) ---
builder.Services.AddHttpClient<IWeatherService, OpenWeatherService>();
builder.Services.AddHttpClient("social-sharing");

// --- Configuração / Options ---
builder.Services.Configure<List<SocialNetworkOption>>(builder.Configuration.GetSection("SocialNetworks"));

// --- Serviços REST (Camada de Lógica) ---
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<IBuildingService, BuildingService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IDataPortabilityService, DataPortabilityService>();
builder.Services.AddScoped<ISocialIntegrationService, SocialIntegrationService>();

// ==========================================
// 2. CONSTRUÇÃO DA APP
// ==========================================
var app = builder.Build();

// ==========================================
// 3. PIPELINE DE PEDIDOS (Middleware)
// A ORDEM AQUI É CRÍTICA! NÃO MUDAR!
// ==========================================

// A. Ambiente de Desenvolvimento
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

// B. Routing (Descobrir qual o endpoint)
app.UseRouting();

// CORS - permitir pedidos do front-end externo
app.UseCors("AllowFrontend");

// C. Segurança (Quem és? Podes entrar?)
app.UseAuthentication();
app.UseAuthorization();

// D. Endpoints Finais (Executar a lógica)
app.UseEndpoints(endpoints =>
{
    // 1. Endpoint SOAP
    endpoints.UseSoapEndpoint<IIncidentService>("/Service.asmx",
        new SoapEncoderOptions(), SoapSerializer.XmlSerializer);

    // 2. Endpoints REST (Controllers)
    endpoints.MapControllers();
});

app.Run();
