# SOCIETIZA — Contexto do Projeto

> Documento de referência sobre o produto Societiza: o que é, para quem é, como funciona e como foi construído.
> Atualizado em 29/03/2026.
> Para uso do Claude: anexe ao Projeto no Chat. Para uso humano: leitura onboarding e alinhamento de contexto.

---

## 1. PRODUTO

### O que é

Societiza é um **workflow societário SaaS** voltado para contabilidades que gerenciam abertura, alteração e baixa de empresas no Brasil. Funciona 100% na nuvem, sem instalação.

- **Sede:** Joinville, SC
- **Site:** societiza.com.br
- **Tagline:** "A maior infraestrutura societária do Brasil."

### Problema que resolve

Contabilidades que fazem abertura de empresas gerenciam dezenas de processos em paralelo — cada um com múltiplas etapas, prazos, portais de prefeitura e documentos diferentes. Sem uma ferramenta dedicada, o controle vira planilha, WhatsApp e memória. Processos travam, clientes ficam sem resposta e a contabilidade perde produtividade.

### Proposta de valor

Um kanban societário 100% customizável que organiza, automatiza e acompanha todo o processo de abertura de empresas. O sistema se adapta ao jeito de trabalhar de cada contabilidade — não o contrário.

---

## 2. PÚBLICO-ALVO

| Perfil | Descrição |
|---|---|
| Contador independente | Atende poucos clientes, precisa de organização sem burocracia |
| Escritório pequeno | Time de 2–5 pessoas, processos começam a se perder |
| Escritório médio/grande | Time especializado em societário, precisa de visibilidade e controle centralizado |

Qualquer profissional que lide com processos societários no Brasil é público potencial.

---

## 3. FUNCIONALIDADES CORE

| Funcionalidade | Descrição |
|---|---|
| Kanban customizável | Colunas, etapas e status configuráveis por escritório; templates prontos para começar |
| Formulário de entrada | Link compartilhável para o cliente gerar um card diretamente no kanban |
| Checklist de tarefas | Tarefas por etapa, com automações em portais de prefeitura (viabilidade, alvará etc.) |
| Notificações por evento | E-mails automáticos para o contador e o cliente a cada mudança relevante no processo |
| Alertas inteligentes | Aviso automático quando um processo fica parado além do prazo esperado |
| Multi-estado nativo | Adapta checklists e automações às regras de cada prefeitura |
| Transparência para o cliente | Acompanhamento em tempo real do status do processo — menos WhatsApp, mais confiança |

---

## 4. TOM DE COMUNICAÇÃO

Direto, profissional e acessível. O público não tem tempo a perder. A comunicação deve ser prática, orientada a resultado e livre de jargão técnico desnecessário.

**Diferenciais a reforçar nos textos:**

| Diferencial | Como expressar |
|---|---|
| Kanban customizável | "O sistema se adapta ao seu jeito de trabalhar, não o contrário" |
| Multi-estado | "Funciona para qualquer prefeitura do Brasil, com checklists específicos por município" |
| Transparência | "Seu cliente acompanha em tempo real — menos WhatsApp, mais confiança" |
| Alertas | "Nenhum processo fica esquecido — o sistema avisa quando algo trava" |

---

## 5. DOMÍNIO — GLOSSÁRIO

| Termo | Definição |
|---|---|
| **Processo** | Uma abertura, alteração ou baixa de empresa em andamento |
| **Card** | Representação de um processo no Kanban |
| **Etapa / Coluna** | Fase do workflow (ex: Viabilidade, Registro, Alvará) |
| **Escritório** | Empresa de contabilidade cliente do Societiza (tenant raiz do sistema) |
| **Operador / Usuário** | Contador que opera o sistema |
| **Cliente final** | Empresário que está abrindo ou alterando a empresa |
| **Viabilidade** | Consulta prévia na prefeitura para verificar se o CNAE é permitido no endereço |
| **Alvará** | Licença de funcionamento emitida pela prefeitura |
| **Template** | Conjunto de etapas e tarefas pré-configuradas para um tipo de processo |

---

## 6. REGRAS DE NEGÓCIO

- Um processo sempre pertence a um escritório — nunca existe solto no sistema.
- Um escritório pode ter múltiplos usuários.
- Notificações são sempre disparadas por evento — nunca por polling.
- Dados do cliente final nunca são expostos para outros escritórios.
- E-mail nunca é enviado diretamente por handlers de negócio — sempre via Hangfire.
- Um `WorkflowTemplate` em status `Draft` não pode ser usado para criar processos.
- Um `User` só pode ser criado a partir de um convite aceito (`UserInvite`) — não existe criação direta.

---

## 7. STACK TÉCNICA

### Backend

| Camada | Tecnologia |
|---|---|
| Linguagem / Framework | C# / .NET / ASP.NET Core |
| Arquitetura | Event-Driven Modular Monolith com DDD, CQRS e Clean Architecture |
| API | Ardalis API Endpoints (uma classe por rota) |
| CQRS | MediatR — Commands mutam estado, Queries leem do MongoDB |
| Banco transacional | PostgreSQL via EF Core (Unit of Work + Repository) |
| Banco de leitura | MongoDB (projeções por módulo) |
| Event bus | RabbitMQ via MassTransit |
| Background jobs | Hangfire (persistido no PostgreSQL, bridge via MediatR) |
| Email | Brevo via SMTP nativo (`System.Net.Mail` — sem SDK externo) |
| Infra local | Docker Compose |

### Estrutura do projeto

```
src/
├── Core/       # Infraestrutura cross-cutting (compartilhada por todos os módulos)
├── Modules/    # Módulos de feature (Accountancy, Identity, WorkflowTemplate)
└── Web/        # Host ASP.NET Core (entry point, endpoints, Program.cs)
tests/          # Todos os projetos de teste
```

Cada módulo tem 5 camadas: `Domain → Application → Infrastructure → Persistence → Shared`

---

## 8. PADRÕES DE CÓDIGO

- Módulos registram serviços via `IServiceInstaller` (descoberto por reflexão).
- Core registra via `InfrastructureServiceInstaller` com registros explícitos — sem Scrutor scan.
- Commands de email contêm apenas tipos primitivos, garantindo serialização pelo Hangfire.
- `IEmailService` não conhece conceitos de domínio — isso é responsabilidade de cada módulo.
- Novos endpoints: criar em `src/Web/Endpoints/Modules/{Module}/`.
- Todas as rotas têm prefixo `/api`.
- Enumerações do módulo Identity usam `Ardalis.SmartEnum` com conversões implícitas `string`/`int`.
- `Role` e `Status` trafegam como `string` nos domain events; conversão para SmartEnum ocorre nos handlers `When(...)` dos aggregates.

---

## 9. MODELO DE DOMÍNIO

O modelo de domínio detalhado (aggregates, entities, value objects, domain events, projection models e enumerações) está documentado em **`domain-model.md`**. Este arquivo é a referência canônica — consulte-o sempre que for modelar ou implementar algo relacionado ao domínio.

**Módulos existentes:** Accountancy · Identity · WorkflowTemplate

**Resumo rápido:**

| Tipo | Itens |
|---|---|
| Aggregates | Accountancy, User, UserInvite, WorkflowTemplate |
| Domain Events | 6 (5 de UserInvite + 1 de User) |
| Projection Models | ProjectionModel.User, ProjectionModel.UserInvite |
| Enumerações | UserRole, InviteStatus, TemplateStatus, TaskType, FieldType |

---

*Atualizado em 29/03/2026.*