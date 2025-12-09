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

## Partilha de incidentes em redes sociais
O endpoint `POST /api/social/incidents/{id}/share` envia um POST real para a configuração definida em `SocialNetworks` no `appsettings.json` (ex.: webhook de integração, API externa):

- **id**: identificador do incidente (via path parameter).
- **network** (obrigatório, body): nome da rede configurada em `SocialNetworks` (ex.: `"twitter"`).
- **message** (opcional, body): texto personalizado até 240 caracteres. Se omitido, a API cria uma mensagem padrão com o tipo e estado do incidente.

Exemplo de configuração mínima:

```json
"SocialNetworks": [
  {
    "Name": "twitter",
    "ApiUrl": "https://postman-echo.com/post",
    "ApiKey": "<TOKEN>",
    "Enabled": true
  }
]
```

Exemplo de chamada com `curl` (JWT necessário no header `Authorization`):

```bash
curl -X POST "https://localhost:5001/api/social/incidents/12/share" \
     -H "Authorization: Bearer <TOKEN>" \
     -H "Content-Type: application/json" \
     -d '{
           "network": "twitter",
           "message": "Atualização do incidente #12 no edifício principal."
         }'
```

Resposta esperada:

```json
{
  "network": "twitter",
  "payloadPreview": "{\"incidentId\":12,\"building\":\"<nome>\",\"status\":\"Open\",\"message\":\"Atualização...\",\"network\":\"twitter\"}",
  "shareUrl": "https://postman-echo.com/post",
  "sentAtUtc": "2024-05-20T10:15:45.123Z",
  "externalStatusCode": 200,
  "externalResponsePreview": "{...resposta da API externa...}"
}
```
