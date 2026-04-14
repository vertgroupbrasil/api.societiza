---
name: spec-engineer
description: >
  Traduz uma spec de produto em um plano técnico de implementação. Lê
  `spec.md` (produzido por `read-spec`), cruza com as convenções de
  arquitetura do projeto, e gera um plano sequencial em
  `specs/{feature}/plan.md` — cada artefato com camada, caminho, dependências
  e spec-ref. Nenhum código é escrito; o humano aprova antes da implementação.
---

# spec-engineer

## Propósito

Uma spec de produto responde **o quê** e **o porquê** — escrita na linguagem
do usuário, focada em intenção, regras de negócio e valor entregue. Ela não
diz onde o código mora, quais arquivos nascem, ou em que ordem as camadas
são tocadas.

Esta skill cobre exatamente essa lacuna: transforma a spec aprovada em um
**plano técnico de implementação** — a tradução fiel do *o quê* para o *como*,
na linguagem de quem vai escrever o código.

Concretamente, a skill:

1. **Lê o artefato de entrada** produzido pelo command `read-spec`:
   - `spec.md` — descrição funcional, regras de negócio, critérios de aceite
2. **Cruza com as convenções do projeto** definidas na skill `/architecture`.
3. **Produz um plano enumerado e sequencial**, onde cada item descreve:
   - o artefato concreto (aggregate, domain event, command, handler, endpoint,
     projection, migration, consumer config, etc.)
   - a camada e o caminho de arquivo onde ele deve nascer ou ser alterado
   - as dependências que precisam estar prontas antes dele
   - o motivo pelo qual ele existe no plano (amarrado a um item da spec)
4. **Ordena os passos respeitando as dependências reais** entre camadas e
   entre módulos.
5. **Persiste o plano em `specs/{feature}/plan.md`** na raiz deste
   repositório e apresenta ao humano para revisão. O plano segue o formato
   definido em `references/plan-template.md`.

Nenhum código é escrito por esta skill. O humano revisa o plano, aponta
ajustes, aprova — e só então a implementação começa. O `plan.md` permanece
no repositório como rastro histórico: spec de produto → plano técnico →
código.

---

## Dependência: skill `/architecture`

As convenções, patterns e restrições de cada camada estão documentadas na
skill `/architecture` (`.claude/skills/architecture/SKILL.md`). Essa skill
é a **fonte de verdade** para classificar artefatos, resolver dependências
e validar o plano. **Não duplique o conteúdo dela aqui.**

Antes de gerar qualquer plano, leia `/architecture` por completo. De lá
você obterá:

- **Write-side chain** completa (Command → ... → Endpoint)
- **Read-side chain** completa (Projection Model → ... → Endpoint GET)
- **Email pattern** (Email Command → Email Handler → Enqueue)
- **Registros obrigatórios** por artefato novo
- **Restrições absolutas** de cada camada ("What to NEVER do")

---

## Como ler a spec de produto

Leia `spec.md` e extraia explicitamente, na linguagem do projeto (DDD + CQRS +
Event Sourcing + leitura via projections):

**Modelo de domínio** — classifique cada conceito da spec conforme os patterns
definidos na skill `/architecture`:
- **Aggregate** — raiz de consistência, expõe métodos que levantam eventos
- **Entity** — tem identidade, vive dentro de um aggregate
- **Value Object** — sem identidade, imutável
- **Domain Event** — fato no passado, mudança de estado do aggregate
- **Projection Model** — representação de leitura em MongoDB

**Comportamentos (write side)** — cada ação que muda estado do sistema vira
a cadeia completa de write-side (definida em `/architecture`). Identifique
cada elo da cadeia para cada comportamento da spec.

**Leituras (read side)** — cada dado exibido na spec vira a cadeia completa
de read-side. Se a spec descreve uma tela ou dado novo, identifique quais
eventos precisam ser projetados para alimentá-la — e, se o projection model
não existe, ele entra no plano antes da query.

**Estados possíveis** — campos com conjunto fixo de valores viram `SmartEnum`.
Siga as regras de serialização definidas em `/architecture`.

**Regras de negócio** — classifique cada regra pela camada correta, conforme
a classificação definida em `/architecture`:
- Formato/estrutura do input → `Validator` via FluentValidation
- Precondição sobre estado, autorização, unicidade → `Handler`
- Invariante do domínio → transição de estado no aggregate (não é validação)

**Emails e notificações** — cada menção vira os três artefatos do email
pattern (definido em `/architecture`).

**Infraestrutura e registros obrigatórios** — se a spec introduz artefatos
novos, consulte a checklist de `/architecture` para garantir que todos os
registros de bootstrap entrem no plano.

**Comunicação entre módulos** — módulos não se referenciam diretamente.
A integração é sempre por domain event consumido no outro módulo. Se a spec
descrever acoplamento direto entre módulos, isso é um sinal e deve virar um item
explícito do plano: "publicar evento X em módulo A, consumir em módulo B
via projection/handler".

---

## Como derivar impacto em módulos e aggregates

A análise de impacto vem de duas fontes: a própria spec e o estado atual do
código.

**Da spec, extraia:**

- **Módulos afetados** — quais dos módulos existentes são tocados? Há
  **módulo novo**? Módulo novo implica criar os 5 projetos da estrutura,
  registrar `DbContext`, criar migration inicial e conectar no host.
- **Aggregates impactados** — novos ou existentes? Se existentes, quais
  métodos/eventos novos? Se novos, todos os artefatos de bootstrap entram
  no plano (EF configs, migration, projection model inicial).
- **Eventos cross-module** — há eventos que saem de um módulo e precisam ser
  consumidos por outro? Cada consumo vira um `ConfigureEventReceiveEndpoint`
  e um projection/event handler no módulo consumidor.

**Do código (verificado no Passo 2), confirme:**

- **Projections afetadas** — quais projections existentes precisam ser
  estendidas? Alterações de schema em projection exigem estratégia de
  rebuild/backfill, que deve aparecer como item do plano.
- **Migrations quebrando** — alguma mudança de schema no write side exige
  migration? Ela entra no plano **antes** do endpoint que depende dela.
- **Pontos de registro a atualizar** — `ConsumerConfiguration`,
  `AddEventInteractors`, `MigrateDatabaseStartupTask`,
  `RegisterRecurringJobsStartupTask`. Marque explicitamente cada um.

---

## How to Use This Skill

Antes de gerar qualquer plano, leia os artefatos de entrada e as referências
de arquitetura. Nunca gere o plano com base em convenções genéricas de .NET —
sempre siga os padrões documentados neste projeto.

1. Leia o artefato da spec ativa (produzido pelo command `read-spec`):
   - `spec.md` — descrição funcional, regras de negócio, critérios de aceite
2. Leia a skill `/architecture` por completo (`.claude/skills/architecture/SKILL.md`).
3. Leia o template do plano em `references/plan-template.md` (desta skill).
4. Execute o procedimento descrito na seção "Procedimento de execução" abaixo.

---

## Procedimento de execução

### Passo 1 — Extração dos artefatos da spec

Aplique a seção "Como ler a spec de produto" e "Como derivar impacto em
módulos e aggregates" para produzir uma lista intermediária (não publicada) com:

- Todos os aggregates (novos e existentes) tocados
- Todos os domain events novos
- Todos os SmartEnums novos
- Todos os comportamentos (write side): cadeia completa por comportamento
- Todas as leituras (read side): cadeia completa por leitura
- Todos os emails/notificações: cadeia completa por email
- Todos os registros de infraestrutura: migrations, EF configs, consumer registrations, etc.
- Todos os eventos cross-module e a direção (módulo publicador → módulo consumidor)

### Passo 2 — Cruzamento com o estado atual do código

Antes de gerar o plano, verifique o estado atual do repositório:

1. **Aggregates existentes** — leia os arquivos em
   `src/Modules/{Module}/{Module}.Domain/` para confirmar quais métodos e
   eventos já existem. Não planeje artefatos que já estão implementados.
2. **Projections existentes** — leia
   `src/Modules/{Module}/{Module}.Domain/Projections/` para verificar se o
   projection model precisa ser criado ou apenas estendido.
3. **DomainEvents.cs** — leia o arquivo de eventos do módulo para confirmar
   quais eventos já existem e evitar duplicação.
4. **ConsumerConfiguration.cs** e **AddEventInteractors()** — confirme quais
   registros já existem para não duplicar.
5. **Routes** — leia `src/Web/Endpoints/Routes/{Module}Routes.cs` para
   verificar rotas existentes e evitar colisão.

### Passo 3 — Montagem do plano

Com a lista de artefatos extraída e o estado atual verificado:

1. **Classifique cada artefato** por camada e tipo conforme `/architecture`.
2. **Resolva as dependências** entre artefatos — cada item deve declarar
   explicitamente de quais outros itens depende. Use as regras de ordenação
   definidas em `references/plan-template.md § Regras de ordenação`.
3. **Numere sequencialmente** respeitando a topologia de dependências.
4. **Amarre cada item à spec** — o campo `Spec-ref` deve apontar para a
   seção ou regra da spec que justifica a existência do artefato.
5. **Preencha a checklist de registro** — marque todos os pontos de
   bootstrap que serão tocados e descreva o que será adicionado em cada um.
6. **Liste riscos e pontos de atenção** — red flags encontrados na spec,
   ambiguidades, necessidade de backfill, mudanças fora do escopo.

### Passo 4 — Persistência e apresentação

1. Crie o diretório `specs/{feature}/` na raiz do repositório (se não existir).
2. Persista o plano em `specs/{feature}/plan.md`, seguindo rigorosamente o
   formato definido em `references/plan-template.md`.
3. Apresente o plano completo ao humano para revisão.
4. Aguarde aprovação explícita antes de qualquer implementação.

---

## Regras de validação do plano

Antes de persistir o `plan.md`, valide que ele respeita todas as regras
abaixo. Se qualquer regra for violada, corrija o plano antes de apresentar.

### Completude

- Todo comportamento (write) da spec tem a cadeia completa de write-side
- Toda leitura (read) da spec tem a cadeia completa de read-side
- Todo email mencionado na spec tem os três artefatos do email pattern
- Todo aggregate novo tem os artefatos de bootstrap:
  EF StoreEventConfiguration + SnapshotConfiguration + Migration
- Todo domain event novo tem um `ConfigureEventReceiveEndpoint` correspondente
- Todo projection handler novo está listado para registro em `AddEventInteractors()`

### Consistência com as convenções

Valide contra as regras "What to NEVER do" de cada camada em `/architecture`.
Em particular:

- Nenhum artefato do plano lê do PostgreSQL no read side
- Nenhum domain event transporta SmartEnum como tipo — sempre `string`
- Nenhum email command contém tipos não-primitivos
- Nenhum endpoint retorna entidade de domínio — sempre Response DTO
- Nenhum módulo referencia outro módulo diretamente — integração sempre via events
- Nenhuma validação de regra de negócio está atribuída ao Domain — sempre no Handler
- Toda rota segue o padrão `v{version:apiVersion}/{module}/...` sem `/api`

### Ordenação

- Todo item Domain aparece antes de qualquer item Application que o usa
- Todo item Shared aparece antes de qualquer item Application que o consome
- Toda Migration aparece antes do endpoint que depende da coluna/tabela nova
- Todo Domain Event aparece antes do Projection Handler que o consome
- Todo Projection Model aparece antes da Query que o lê
- Todo Email Command aparece antes do Handler de negócio que o enfileira
- Todo Consumer Registration aparece depois do Domain Event que configura

---

## Nunca fazer

- Gerar código — esta skill produz apenas o `plan.md`
- Assumir que artefatos existem sem verificar no repositório
- Omitir artefatos de infraestrutura (migrations, EF configs, consumer registrations)
- Misturar itens de módulos diferentes sem explicitar a fronteira cross-module
- Criar itens genéricos ("implementar validações") — cada item deve ser concreto
- Incluir refactors fora da spec sem listá-los em "Riscos e pontos de atenção"
- Ignorar ambiguidades da spec — toda ambiguidade vira ponto de atenção
- Escrever o plano sem ter lido o estado atual do código
- Duplicar conteúdo da skill `/architecture` — referenciar

## Sempre fazer

- Executar `read-spec` antes desta skill (se o artefato não estiver no contexto)
- Ler a skill `/architecture` antes de gerar o plano
- Verificar o código existente antes de planejar cada artefato
- Seguir rigorosamente o formato de `references/plan-template.md`
- Declarar dependências explícitas entre itens (`Depende de: #N, #M`)
- Amarrar cada item a uma seção da spec (`Spec-ref`)
- Preencher a checklist de registro por completo — o humano precisa ver que foram considerados
- Listar riscos e pontos de atenção — mesmo que não haja, escrever "Nenhum"
- Apresentar o plano e aguardar aprovação explícita antes de prosseguir