[![Review Assignment Due Date](https://classroom.github.com/assets/deadline-readme-button-22041afd0340ce965d47ae6ef1cefeee28c7c493a6346c4f15d667ab976d596c.svg)](https://classroom.github.com/a/38tL-Jy9)

# Trabalho Prático II — SafeHome
Integração de Sistemas de Informação  
Licenciatura em Engenharia de Sistemas Informáticos (regime *laboral*) 2025–26


---

## Constituição do grupo
| número | nome         | email                 |
|------:|--------------|-----------------------|
| 22997 | Fábio Costa  | a22997@alunos.ipca.pt |
| 23015 | Lino Azevedo | a23015@alunos.ipca.pt |

---

## Descrição do problema
**Tema:** SafeHome  
**Descrição:** Sistema de monitorização de segurança para edifícios residenciais em zonas de risco (incêndios ou cheias).

A solução é composta por:
- **API (backend / Web API)** com documentação Swagger/OpenAPI
- **Frontend** com páginas web estáticas
- **Swagger Client externo** (Swagger UI fora da API), a consumir a especificação OpenAPI da API

---

## Deployment (Azure)
A base de dados e a API estão alojadas no Azure.

- **API (Azure App Service):**  
  https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net

- **Swagger UI (na própria API):**  
  https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net/swagger

- **OpenAPI (JSON) para utilização externa:**  
  https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net/swagger/v1/swagger.json

---

## Organização do repositório
- `ER/` — artefactos do modelo entidade–relação
- `doc/` — documentação/relatório do trabalho
- `src/` — código-fonte e artefactos do projeto
  - `src/API/` — backend (solução em `src/API/SafeHome_Solution/`) + artefactos de publicação (`publish/`, `publish.zip`)
  - `src/Frontend/` — páginas estáticas (login, registo, dashboard, estilos)
  - `src/swagger-client/` — Swagger UI externo (cliente), a consumir o OpenAPI da API

---

# Como colocar a aplicação em funcionamento (passo a passo)

## Opção A — Usar os serviços já alojados no Azure (recomendado)
1. Abrir a documentação da API (Swagger UI):
   - https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net/swagger

2. Confirmar que a especificação OpenAPI está acessível:
   ```bash
   curl -L "https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net/swagger/v1/swagger.json"
   ```

3. Testar endpoints no Swagger UI:
   - Selecionar um endpoint → **Try it out** → **Execute**
   - Confirmar respostas e persistência (quando aplicável)

---

## Opção B — Executar localmente (API + Frontend + Swagger Client externo)

### 1) Pré-requisitos
- Git
- .NET SDK (recomendado: 8.0 ou superior)
- Python 3 (para servir o frontend e o swagger-client)

### 2) Clonar o repositório
```bash
git clone https://github.com/fabiocosta191/Projeto_ISI_Tb2.git
cd Projeto_ISI_Tb2
```

### 3) Executar a API localmente
```bash
cd src/API/SafeHome_Solution
dotnet restore
dotnet build
dotnet run
```

Depois de iniciar, a consola indica o URL (por exemplo `https://localhost:5001`).  
Se estiver configurado, a documentação fica disponível em:
- `https://localhost:<porta>/swagger`
- `https://localhost:<porta>/swagger/v1/swagger.json`

### 4) Executar o Frontend (páginas estáticas)
```bash
cd src/Frontend
python -m http.server 5500
```

Abrir no browser (exemplos):
- http://localhost:5500/login.html
- http://localhost:5500/register.html
- http://localhost:5500/dashboard.html

### 5) Executar o Swagger Client externo (fora da API)
O Swagger Client é um Swagger UI separado da API e carrega o OpenAPI (swagger.json) para permitir consulta e testes.

```bash
cd src/swagger-client
python -m http.server 8080
```

Abrir:
- http://localhost:8080/index.html

Para apontar explicitamente para o OpenAPI do Azure:
- http://localhost:8080/index.html?spec=https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net/swagger/v1/swagger.json

### 6) Testes (se existirem no projeto)
A partir da raiz do repositório:
```bash
dotnet test
```

---

## CORS (quando usar o Swagger Client externo)
Ao testar “Try it out” a partir do Swagger UI externo, o browser pode bloquear pedidos por CORS se a API não permitir chamadas a partir de outro domínio.

Alternativas práticas:
- Usar o Swagger UI da própria API (em Azure ou local), que corre no mesmo domínio/porta da API
- Testar endpoints com `curl` ou Postman
- Servir o Swagger UI externo atrás de um proxy no mesmo domínio (quando aplicável)

---

## Recursos
- Enunciado: `ESI-ISI 2025-26 - TP2 - enunciado.pdf`
- OpenAPI (JSON):  
  https://safehomeapi-22997-23015-h8dufye2amezh5f4.spaincentral-01.azurewebsites.net/swagger/v1/swagger.json

---

## Autores
Fábio Costa (22997)  
Lino Azevedo (23015)
