# src — SafeHome (TP02 22997_23015)

Esta pasta contém o código e artefactos do projeto, organizado em 3 partes:
- **API** (backend / Web API)
- **Frontend** (páginas web estáticas)
- **swagger-client** (Swagger UI **fora** da API, a consumir o OpenAPI da API)

> Objetivo: manter o Swagger **dentro da API** (como documentação) e, ao mesmo tempo, ter um **cliente Swagger externo** para cumprir o requisito do professor (“usar Swagger como cliente fora da API”), sem alterar a API.

---

## Estrutura

```
src/
├─ API/
│  ├─ SafeHome_Solution/     # solução/projeto do backend (fonte)
│  ├─ publish/               # output de publicação (build)
│  └─ publish.zip            # pacote de publicação (ex: deploy)
├─ Frontend/
│  ├─ login.html
│  ├─ register.html
│  ├─ dashboard.html
│  └─ styles.css
├─ swagger-client/
│  ├─ index.html             # Swagger UI externo (cliente)
│  ├─ docker-compose.yml     # opcional (Swagger UI via Docker)
│  └─ README.md              # instruções do swagger-client
└─ README.md                 # (este ficheiro)
```

---

## API (Backend)

A pasta `API/` contém o backend do projeto (solução dentro de `SafeHome_Solution/`) e também artefactos de publicação (`publish/` e `publish.zip`).

### Executar localmente (forma típica)
1. Abrir a solução no Visual Studio **ou** usar CLI:
   ```bash
   cd src/API/SafeHome_Solution
   dotnet restore
   dotnet build
   dotnet run
   ```

2. Depois de iniciar, a documentação Swagger normalmente fica em:
   - `https://localhost:<porta>/swagger`
   - e o OpenAPI em algo como:
     - `https://localhost:<porta>/swagger/v1/swagger.json`

> Nota: as portas variam conforme a configuração do projeto.

### Artefactos de deploy
- `publish/` e `publish.zip` são outputs de publicação (úteis para deploy em Azure/App Service).
- Se precisares de gerar novamente:
  ```bash
  dotnet publish -c Release -o publish
  ```

---

## Frontend (Web)

A pasta `Frontend/` tem páginas estáticas (HTML/CSS):
- `login.html`
- `register.html`
- `dashboard.html`
- `styles.css`

### Como abrir/testar
Dá para abrir diretamente no browser, mas o ideal é servir com um servidor simples (evita problemas com caminhos/requests):

**Opção A — Python**
```bash
cd src/Frontend
python -m http.server 5500
```
Abrir: `http://localhost:5500/login.html` (ou outra página)

**Opção B — VS Code Live Server**
- Abrir a pasta `Frontend/` no VS Code
- Clicar com botão direito → **Open with Live Server**

---

## Swagger Client (fora da API)

A pasta `swagger-client/` é um Swagger UI **separado da API** (cliente externo).  
Ele **não altera** o backend — apenas carrega o ficheiro OpenAPI (swagger.json) e permite testar endpoints a partir do browser.

### API (Azure) usada como alvo
- Base URL:
  - `https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net`
- OpenAPI (padrão do cliente):
  - `.../swagger/v1/swagger.json`

### Executar o swagger-client
```bash
cd src/swagger-client
python -m http.server 8080
```

Abrir no browser:
- `http://localhost:8080/index.html`

Se o swagger.json estiver noutro caminho, usa:
- `http://localhost:8080/index.html?spec=URL_DO_SWAGGER_JSON`

### Nota sobre CORS
Se o “Try it out” falhar com erro de **CORS**, isso é normal quando a API não permite requests a partir de outro domínio.
Sem mexer na API, as opções típicas são:
1. usar o Swagger UI externo só como **documentação**, ou
2. testar com **Postman/curl**, ou
3. servir UI + proxy no mesmo domínio (ex: Nginx/Caddy) para evitar CORS.

---

## Como avaliar rapidamente se está tudo OK

- **API no Azure** responde?
  - abrir no browser: `https://.../swagger`
  - abrir o json: `https://.../swagger/v1/swagger.json`

- **Swagger externo** abre?
  - `http://localhost:8080/index.html`

- **Frontend** abre?
  - `http://localhost:5500/login.html`

---

## Autores / Contexto
Trabalho académico (TP02) — organização do projeto em backend (API), frontend e um cliente Swagger externo.
