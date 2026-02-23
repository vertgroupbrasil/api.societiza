# MeuSocietário

## Executar o projeto localmente

### 1. Subir a infraestrutura local

```bash
docker-compose -f Docker/docker-compose.Infrastructure.Development.yaml up -d
```

### 2. Configurar credenciais

Copie o arquivo de exemplo e preencha com suas credenciais:

```bash
cp .env.example .env.local
```

Para desenvolvimento local, os valores padrão já funcionam sem alteração.

Os serviços estarão disponíveis nas portas:
- **PostgreSQL:** 5432
- **MongoDB:** 27017
- **RabbitMQ:** 5672

### 3. Buildar a imagem da API

```bash
docker build -t meusocietario-api .
```

### 4. Executar a API

```bash
docker run --rm -p 5258:8080 \
  --network meusocietario-dev \
  --env-file .env.local \
  meusocietario-api
```

- API: **http://localhost:5258/api**

- Swagger: **http://localhost:5258/api/swagger**
