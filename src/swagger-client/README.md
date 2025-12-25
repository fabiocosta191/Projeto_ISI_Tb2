# Swagger Client (fora da API) — SafeHome API

Isto cria um Swagger UI **separado** (um "cliente" fora da API) que consome o **OpenAPI** exposto pela tua API em Azure:

- API base: https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net
- OpenAPI (tentativa padrão): `https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net/swagger/v1/swagger.json`

## 1) Descobrir o URL certo do OpenAPI (swagger.json)
Se `https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net/swagger/v1/swagger.json` não abrir no browser, tenta (por esta ordem):

- `https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net/swagger/v1/swagger.json`
- `https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net/swagger/swagger.json`
- `https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net/openapi.json`
- `https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net/api-docs`
- `https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net/swagger/index.html` (página) — a partir daí normalmente encontras o `.json`

Quando encontrares, usa-o no `?spec=`.

## 2) Executar localmente (o mais simples)

### Opção A — Python
Na pasta deste projeto:
```bash
python -m http.server 8080
```
Abre:
- `http://localhost:8080/index.html`

Se o teu swagger.json tiver outro caminho:
- `http://localhost:8080/index.html?spec=COLOCA_AQUI_O_URL_DO_SWAGGER_JSON`

### Opção B — Node (npx)
```bash
npx serve . -l 8080
```

## 3) Executar via Docker (Swagger UI pronto)
```bash
docker run --rm -p 8080:8080 \
  -e API_URL="https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net/swagger/v1/swagger.json" \
  swaggerapi/swagger-ui
```

> Nota: algumas versões do container usam `API_URL`, outras `SWAGGER_JSON`.
> Se não funcionar com `API_URL`, tenta:
> `-e SWAGGER_JSON="https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net/swagger/v1/swagger.json"`

## 4) Se o "Try it out" der erro de CORS
Isto acontece quando a API não permite chamadas a partir de outro domínio (normal em produção).

Sem mexer na API, tens 3 saídas:
1. Usar o Swagger UI só como **documentação** (sem executar requests no browser);
2. Testar chamadas com **curl/Postman/Insomnia** (não depende de CORS);
3. Montar um **proxy** (Nginx/Caddy) para servir o UI e encaminhar chamadas para a API (evita CORS por ser same-origin).
