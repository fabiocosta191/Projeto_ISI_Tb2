# SafeHome Platform

Este repositório contém a API, data layer e testes automatizados do projeto SafeHome.

## Componentes
- **SafeHome.API** – API REST (JWT) e serviço SOAP para incidentes.
- **SafeHome.Data** – Camada de dados com EF Core.
- **SafeHome.Tests** – Testes xUnit com base de dados in-memory.

## Configuração
1. Atualiza `API/SafeHome_Solution/SafeHome.API/appsettings.json` com:
   - Connection string para SQL Server.
   - Valores de `Jwt:Key`, `Jwt:Issuer` e `Jwt:Audience`.
   - Chave `OpenWeather:ApiKey` para a integração meteorológica.
2. Executa as migrações/actualização da BD conforme necessário.

## Execução
```bash
cd API/SafeHome_Solution/SafeHome.API
DOTNET_ENVIRONMENT=Development dotnet run
```
Aceder ao Swagger em `https://localhost:5001/swagger` (ou porta configurada) para testar os endpoints REST e ao serviço SOAP em `/Service.asmx`.

## Testes
```bash
cd API/SafeHome_Solution
dotnet test
```

## Cobertura funcional
- Endpoints REST completos para edifícios, sensores, leituras, alertas, incidentes e utilizadores.
- Serviço SOAP com operações de criação/listagem/actualização/remoção de incidentes.
- Autenticação JWT com hashing de passwords (BCrypt).
- Integração externa com OpenWeather (via `IWeatherService`).
