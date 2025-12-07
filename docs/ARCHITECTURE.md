# Arquitetura e Deploy

## Camadas
- **API (REST + SOAP)**: Controllers REST para CRUD completo de edifícios, sensores, leituras, alertas, incidentes e utilizadores. Serviço SOAP (`/Service.asmx`) expõe operações equivalentes para incidentes.
- **Serviços**: Classes `*Service` encapsulam a lógica de negócio e acesso à base de dados.
- **Data Layer**: `SafeHome.Data` com `AppDbContext` e modelos EF Core.

## Autenticação
- JWT Bearer configurado em `Program.cs` com emissor, audiência e chave definidos em `appsettings.json`.
- Passwords são hashed com BCrypt no registo e nas operações de reset.

## Integrações externas
- `OpenWeatherService` usa `HttpClient` e a chave `OpenWeather:ApiKey` para enriquecer a resposta de edifícios com meteorologia.

## Testes
- `SafeHome.Tests` usa EF InMemory para validar CRUD dos serviços de incidentes, alertas e utilizadores.

## Deployment (exemplo Azure App Service)
1. Criar um recurso SQL Database e obter a connection string.
2. Configurar secrets (`Jwt:*`, `ConnectionStrings:DefaultConnection`, `OpenWeather:ApiKey`) como Application Settings no App Service.
3. Fazer deploy do projeto `SafeHome.API` com `dotnet publish` ou GitHub Actions para o App Service.
4. Ativar HTTPS Only e, opcionalmente, Identity Provider externo para reforçar autenticação.
