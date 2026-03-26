# SOCIETIZA — Guia de Trabalho com Claude

> Documento de referência para uso do Claude Chat, Cowork e Code no projeto Societiza.
> Gerado em 25/03/2026.

---

## 1. QUAL FERRAMENTA USAR

### Regra geral

**Chat → para pensar. Code → para executar. Cowork → para operar.**

Nunca vá direto ao Code para uma feature nova sem ter passado pelo Chat primeiro.
O retrabalho em código custa muito mais tokens e tempo do que uma conversa de modelagem bem feita.

### Tabela de decisão

| Atividade | Ferramenta |
|---|---|
| Modelagem de domínio (entidades, agregados, eventos) | **Chat** |
| Decisão de arquitetura (padrões, trade-offs, ADRs) | **Chat** |
| Desenho de uma feature antes de implementar | **Chat** |
| Copywriting, e-mails, textos do produto | **Chat** |
| Estratégia comercial, pricing, posicionamento | **Chat** |
| Análise de feedbacks de clientes | **Chat** |
| Documentação de features | **Chat** |
| Implementação de feature já modelada | **Code** |
| Debugging, refatoração, revisão de código | **Code** |
| Migrations de banco, testes, novos endpoints | **Code** |
| Organizar arquivos, processar dados, rodar scripts | **Cowork** |
| Tarefas repetitivas no sistema operacional | **Cowork** |

### Fluxo ideal para uma nova feature

```
1. Claude Chat  →  Modelar: entidades, regras, fluxo, contrato de API
2. Salvar resultado como arquivo no Projeto do Chat (ex: feature-x.md)
3. Claude Code  →  Implementar referenciando o arquivo via #feature-x.md
```

---

## 2. COMO CRIAR O PROJETO NO CLAUDE CHAT

### Onde fica
Claude.ai → menu lateral → **Projetos** → **Novo projeto**

### 3 camadas de configuração

**Nome do projeto:**
```
Societiza — Produto & Backend
```

**Descrição do projeto:**
```
Desenvolvimento e gestão do produto Societiza. Cobre backend (.NET, DDD, CQRS), 
estratégia de produto, novas features, comercial e comunicação.
```

**Instruções do projeto** (System Prompt — cole exatamente como está abaixo):

---

```
Você é um assistente especializado no projeto Societiza. Eu sou o responsável pelo projeto como um todo: backend, infraestrutura, estratégia de produto, novas features, inovação e comercial.

## Sobre o Produto
Societiza é um workflow societário SaaS (100% na nuvem, sem instalação) voltado para contabilidades que gerenciam abertura, alteração e encerramento de empresas no Brasil.
- Sede: Joinville, SC
- Site: societiza.com.br
- Tagline: "Seu societário. Seu controle."

## Público-alvo
Contadores independentes, escritórios de contabilidade (pequenos e grandes), times de societário. Qualquer profissional que lide com processos societários no Brasil.

## Proposta de Valor
Organizar, automatizar e acompanhar todo o processo de abertura de empresas em um kanban societário 100% customizável. O sistema se adapta ao jeito de trabalhar de cada contabilidade — não o contrário.

## Funcionalidades Core
- Kanban customizável (colunas, etapas, status, templates prontos)
- Formulário para o cliente gerar card no kanban via link
- Checklist de tarefas + automações em portais de prefeitura (viabilidade, alvará etc.)
- Notificações automáticas por evento (cliente e contabilidade)
- Alertas inteligentes para processos parados
- Multi-estado nativo (adapta-se às regras de cada prefeitura)
- Transparência para o cliente (acompanhamento em tempo real)

## Planos
- Salinha — Gratuito (1 usuário, até 3 processos ativos)
- Escritório — R$ 109,90/mês (até 3 usuários, processos ilimitados)
- Enterprise — Sob consulta (usuários ilimitados, gestão centralizada)

## Tom de Comunicação
Direto, profissional mas acessível. Fala com contadores que não têm tempo a perder. Evita jargão técnico desnecessário. Linguagem prática e orientada a resultado.

## Stack Técnica — Backend
- Arquitetura: Event-Driven Modular Monolith com DDD, CQRS e Clean Architecture
- Linguagem/Framework: .NET / ASP.NET Core
- API: Ardalis API Endpoints (uma classe por rota)
- CQRS: MediatR (Commands mutam estado, Queries leem do MongoDB)
- Banco transacional: PostgreSQL via EF Core (Unit of Work + Repository)
- Banco de leitura: MongoDB (projeções por módulo)
- Event bus: RabbitMQ via MassTransit
- Background jobs: Hangfire (persistido no PostgreSQL, bridge via MediatR)
- Email: Brevo via SMTP nativo (System.Net.Mail — sem SDK externo)
- Infra local: Docker Compose

## Estrutura do Projeto
src/
├── Core/       # Infraestrutura cross-cutting (compartilhada por todos os módulos)
├── Modules/    # Módulos de feature (Accountancy, Identity, WorkflowTemplate)
└── Web/        # Host ASP.NET Core (entry point, endpoints, Program.cs)
tests/          # Todos os projetos de teste

Cada módulo tem 5 camadas: Domain → Application → Infrastructure → Persistence → Shared

## Domínio — Glossário
- Processo: Uma abertura/alteração/encerramento de empresa em andamento
- Card: Representação de um processo no Kanban
- Etapa/Coluna: Fase do workflow (ex: Viabilidade, Registro, Alvará)
- Escritório: Empresa de contabilidade cliente do Societiza
- Operador/Usuário: Contador que usa o sistema
- Cliente final: Empresário que está abrindo/alterando a empresa
- Viabilidade: Consulta prévia na prefeitura para verificar se CNAE é permitido no endereço
- Alvará: Licença de funcionamento emitida pela prefeitura

## Regras de Negócio Críticas
- Um processo sempre pertence a um escritório
- Um escritório pode ter múltiplos usuários (conforme plano)
- Processos ativos no plano Salinha: máximo 3
- Notificações devem ser disparadas por evento, nunca por polling
- Dados do cliente final nunca devem ser expostos para outros escritórios
- Email nunca é enviado diretamente por handlers de negócio — sempre via Hangfire

## Padrões de Código Estabelecidos
- Módulos registram serviços via IServiceInstaller (descoberto por reflexão)
- Core registra via InfrastructureServiceInstaller com registros explícitos (sem Scrutor scan)
- Commands de email contêm apenas tipos primitivos (garantia de serialização pelo Hangfire)
- IEmailService não conhece conceitos de domínio — isso é responsabilidade de cada módulo
- Novos endpoints: criar em src/Web/Endpoints/Modules/{Module}/
- Todas as rotas têm prefixo /api

Sempre que eu não especificar o contexto, assuma que estamos trabalhando dentro do produto Societiza.
Quando a tarefa for técnica, siga os padrões arquiteturais estabelecidos acima.
Quando a tarefa for de produto ou comercial, use o tom e os diferenciais descritos acima.
```

---

**Documentos para anexar ao projeto** (aba de arquivos do Projeto):

| Arquivo | Por quê |
|---|---|
| `SOCIETIZA_CONTEXT.md` | Referência geral de produto e templates de prompt |
| `CLAUDE.md` | Contexto técnico completo do backend |

---

## 3. TEMPLATES DE PROMPT POR CONTEXTO

### Modelagem de domínio

```
Feature: [nome]

Contexto:
[descreva o problema que a feature resolve para o contador]

O que preciso modelar:
- Quais entidades/agregados estão envolvidos?
- Quais são as regras de negócio?
- Quais eventos de domínio devem ser emitidos?
- Como fica o contrato do endpoint (request/response)?

Restrições:
- Seguir os padrões do projeto (DDD, CQRS, Modular Monolith)
- [outras restrições específicas]
```

### Decisão de arquitetura

```
Decisão: [título da decisão]

Contexto:
[por que essa decisão precisa ser tomada agora]

Opções que estou considerando:
1. [opção A]
2. [opção B]

Me ajude a avaliar trade-offs considerando:
- A stack atual (.NET, MassTransit, Hangfire, MongoDB + PostgreSQL)
- O estágio do produto (early stage, time pequeno)
- Escalabilidade futura
```

### Nova feature — estratégia de produto

```
Feature: [nome]

Problema que resolve para o contador:
[descreva em linguagem do usuário]

Quero avaliar:
- Vale construir agora ou é prematura?
- Qual o impacto no plano de preços?
- Como comunicar para o mercado?
- Algum risco ou dependência técnica relevante?
```

### Copywriting / comunicação

```
Peça: [tipo — ex: e-mail de onboarding, post LinkedIn, FAQ]

Objetivo: [o que o leitor deve sentir ou fazer após ler]

Público: [ex: contador que acabou de criar conta no plano Salinha]

Tom: direto, sem jargão, orientado a resultado

Restrições: [ex: máximo 150 palavras / não mencionar concorrentes]
```

---

## 4. APRIMORANDO FEATURES JÁ IMPLEMENTADAS

### O Chat não enxerga o repositório — use o Code

Quando a tarefa depende de entender código existente, padrões já implementados e arquivos reais, o Chat não é suficiente. Ele pode raciocinar sobre arquitetura, mas não consegue navegar pelo seu projeto real.

**Use Claude Code sempre que a tarefa envolver:**
- Alterar comportamento de algo já funcionando
- Entender como um padrão foi implementado antes de seguir o mesmo
- Refatorar código existente
- Debugar um comportamento inesperado
- Adicionar algo dentro de um módulo já criado

### Fluxo para aprimorar uma feature existente

```
Claude Chat   →  "Quero aprimorar X, faz sentido? Quais impactos?" (opcional)
Claude Code   →  Explora o código, mapeia arquivos, implementa
Claude Chat   →  Documenta a decisão tomada (ADR, atualiza feature doc)
```

O Chat continua útil para a decisão estratégica **antes** e para documentação **depois** — mas o meio, onde o código está, é território do Code.

### Como orientar o Claude Code a explorar antes de agir

Antes de descrever a implementação, peça ao Code para mapear o terreno:

```bash
# No terminal, antes de descrever a tarefa:
/explore

# Ou inclua essa instrução no prompt:
"Antes de implementar, mapeie os arquivos envolvidos e me mostre
como o padrão foi implementado em um módulo existente.
Só implemente após minha aprovação do plano."
```

Isso evita que ele invente um padrão novo quando já existe um estabelecido no projeto.

### Template — aprimorar feature existente

```
Funcionalidade: [nome da feature a aprimorar]

Comportamento atual:
[descreva o que já funciona]

O que precisa mudar:
[descreva o aprimoramento com clareza]

Antes de implementar:
1. Mapeie os arquivos envolvidos
2. Identifique um módulo similar já implementado para usar como referência
3. Me mostre o plano de alterações antes de executar

Restrições:
- Seguir exatamente o padrão do módulo de referência
- Não alterar interfaces públicas sem avisar
- [outras restrições específicas]
```

### Exemplo real no contexto do Societiza

```
Funcionalidade: Alerta de processo parado

Comportamento atual:
O sistema já envia notificações por evento (ex: processo avançou de etapa).
Não existe ainda nenhum alerta para processos que ficam sem movimentação.

O que precisa mudar:
Se um processo ficar X dias sem movimentação (X configurável por escritório),
o contador responsável deve receber um e-mail de alerta com a lista de
processos parados.

Antes de implementar:
1. Mapeie os arquivos do módulo de notificações existente
2. Identifique como o Hangfire está sendo usado para jobs recorrentes
3. Mostre o plano antes de executar

Restrições:
- Usar IJobSchedulerService para agendar o job (não criar novo mecanismo)
- Usar o padrão de email do módulo Accountancy como referência
- Não alterar a interface IEmailService
```

---

## 5. DOCUMENTOS RECOMENDADOS PARA CRIAR NO PROJETO (CLAUDE CHAT)

À medida que o projeto evolui, crie e anexe estes arquivos no Projeto do Claude Chat:

| Arquivo | Conteúdo | Quando criar |
|---|---|---|
| `domain-model.md` | Diagrama textual de entidades e relacionamentos | Agora |
| `architecture-decisions.md` | ADRs — decisões tomadas e o porquê | A cada decisão relevante |
| `features/` | Um `.md` por feature modelada no Chat | Antes de cada implementação |
| `api-contracts.md` | Endpoints, payloads, contratos frontend↔backend | Conforme a API cresce |
| `roadmap.md` | Features planejadas com prioridade e justificativa | Quando o roadmap estiver definido |

---

## 6. CHECKLIST ANTES DE ENVIAR UM PROMPT

- [ ] Estou na ferramenta certa? (Chat para pensar, Code para executar)
- [ ] O System Prompt do projeto está ativo? (Projeto configurado no Chat)
- [ ] Descrevi o **problema**, não só a solução que imaginei?
- [ ] Especifiquei o **formato de saída** esperado?
- [ ] Para decisões técnicas: mencionei as **restrições** da stack atual?
- [ ] Para features novas: a feature já foi **modelada no Chat** antes de ir ao Code?
- [ ] Para features existentes: pedi ao Code para **mapear e apresentar o plano** antes de implementar?

---

*Atualizado em 25/03/2026.*
