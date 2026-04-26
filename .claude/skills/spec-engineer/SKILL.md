---
name: spec-engineer
description: >
  Traduz spec.md + logic.md (produzidos por `read-spec`) em um plano técnico
  de implementação. Cruza com as convenções de arquitetura do projeto e gera
  um plano sequencial em `specs/{feature}/plan.md` — cada artefato com camada,
  caminho, dependências e referência à spec. Nenhum código é escrito; o humano aprova
  antes da implementação.
---

# spec-engineer

## Propósito

`spec.md` responde **o quê** e **o porquê** — escrita na linguagem do usuário,
focada em intenção e valor entregue. `logic.md` detalha **como o sistema se
comporta** — regras de negócio numeradas, máquinas de estado, fluxos por perfil
de usuário e casos de borda. Nenhum dos dois diz onde o código mora, quais arquivos
nascem, ou em que ordem as camadas são tocadas.

Esta skill cobre exatamente essa lacuna: transforma os dois artefatos aprovados
em um **plano técnico de implementação** — a tradução fiel do *o quê* e do *como*
para o *onde* e *quando*, na linguagem de quem vai escrever o código.

Concretamente, a skill:

1. **Lê os dois artefatos de entrada** produzidos pelo comando `read-spec`:
   - `spec.md` — escopo, decisões funcionais, fora de escopo
   - `logic.md` — regras de negócio numeradas, estados e transições, fluxos por
     perfil, pontos de integração, casos de borda
2. **Cruza com as convenções do projeto** definidas na skill `/architecture`.
3. **Produz um plano enumerado e sequencial**, onde cada item descreve:
   - o artefato concreto (aggregate, domain event, comando, handler, endpoint,
     projection, migration, configuração de consumer, etc.)
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

As convenções, padrões e restrições de cada camada estão documentadas na
skill `/architecture` (`.claude/skills/architecture/SKILL.md`). Essa skill
é a **fonte de verdade** para classificar artefatos, resolver dependências
e validar o plano. **Não duplique o conteúdo dela aqui.**

Antes de gerar qualquer plano, leia `/architecture` por completo. De lá
você obterá:

- **Cadeia de escrita** completa (Comando → ... → Endpoint)
- **Cadeia de leitura** completa (Projection Model → ... → Endpoint GET)
- **Padrão de email** (Comando de email → Handler de email → Enqueue)
- **Registros obrigatórios** por artefato novo
- **Restrições absolutas** de cada camada ("O que NUNCA fazer")

---

## Como ler a spec de produto (spec.md)

Leia `spec.md` e extraia explicitamente, na linguagem do projeto (DDD + CQRS +
Event Sourcing + leitura via projections):

**Modelo de domínio** — classifique cada conceito da spec conforme os padrões
definidos na skill `/architecture`:
- **Aggregate** — raiz de consistência, expõe métodos que levantam eventos
- **Entity** — tem identidade, vive dentro de um aggregate
- **Value Object** — sem identidade, imutável
- **Domain Event** — fato no passado, mudança de estado do aggregate
- **Projection Model** — representação de leitura em MongoDB

**Comportamentos (lado de escrita)** — cada ação que muda estado do sistema vira
a cadeia completa de escrita (definida em `/architecture`). Identifique
cada elo da cadeia para cada comportamento da spec.

**Leituras (lado de leitura)** — cada dado exibido na spec vira a cadeia completa
de leitura. Se a spec descreve uma tela ou dado novo, identifique quais
eventos precisam ser projetados para alimentá-la — e, se o projection model
não existe, ele entra no plano antes da query.

**Emails e notificações** — cada menção vira os três artefatos do padrão de email
(definido em `/architecture`).

**Infraestrutura e registros obrigatórios** — se a spec introduz artefatos
novos, consulte a checklist de `/architecture` para garantir que todos os
registros de bootstrap entrem no plano.

**Comunicação entre módulos** — módulos não se referenciam diretamente.
A integração é sempre por domain event consumido no outro módulo. Se a spec
descrever acoplamento direto entre módulos, isso é um sinal e deve virar um item
explícito do plano: "publicar evento X em módulo A, consumir em módulo B
via projection/handler".

---

## Como ler o documento de lógica (logic.md)

O `logic.md` é a fonte primária para todas as decisões de comportamento do
sistema. Leia-o **depois** do `spec.md` e extraia:

**Regras de negócio numeradas** — cada regra deve ser classificada pela camada
correta, conforme `/architecture`:
- Formato/estrutura do input → `Validator` via FluentValidation
- Precondição sobre estado, autorização, unicidade → `Handler`
- Invariante do domínio → transição de estado no aggregate (não é validação)

Cada regra do `logic.md` deve aparecer amarrada a pelo menos um artefato do
plano via `Spec-ref`. Regras sem artefato correspondente são um sinal de que
um item está faltando.

**Estados e transições** — cada máquina de estado do `logic.md` se converte em:
- `SmartEnum` com os valores listados (Fase 1 — Domain)
- Método no aggregate que levanta o Domain Event da transição
- `When()` no aggregate que aplica o evento e muda o estado

Se o `logic.md` descreve uma transição que a spec não menciona explicitamente,
ela entra no plano assim mesmo — o documento de lógica tem precedência sobre o fluxo feliz
da spec para modelagem de estado.

**Fluxos por perfil de usuário** — cada perfil (SystemAdmin, AccountancyAdmin,
AccountancyEmployee, etc.) tem ações distintas. Para cada ação por perfil:
- Confirme se há um endpoint correspondente
- Confirme que a autorização está modelada no Handler (verificação de papel/permissão)
- Se o perfil não pode realizar a ação, o Handler deve rejeitar com erro adequado

**Pontos de integração** — a seção de integração do `logic.md` define o contrato
entre o que BE expõe e o que FE precisa receber. Use-a para:
- Confirmar que os DTOs de resposta cobrem todos os dados necessários
- Confirmar que os endpoints mapeiam todos os fluxos descritos
- Identificar dados calculados que precisam ser projetados no MongoDB

**Casos de borda** — cada caso de borda do `logic.md` deve virar um artefato concreto
no plano: uma validação no `Validator`, uma guarda no `Handler`, ou uma nota
em "Riscos e pontos de atenção" se a decisão ainda não está clara.

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
  e um handler de projection/evento no módulo consumidor.

**Do código (verificado no Passo 2), confirme:**

- **Projections afetadas** — quais projections existentes precisam ser
  estendidas? Alterações de schema em projection exigem estratégia de
  rebuild/backfill, que deve aparecer como item do plano.
- **Migrations quebrando** — alguma mudança de schema no lado de escrita exige
  migration? Ela entra no plano **antes** do endpoint que depende dela.
- **Pontos de registro a atualizar** — `ConsumerConfiguration`,
  `AddEventInteractors`, `MigrateDatabaseStartupTask`,
  `RegisterRecurringJobsStartupTask`. Marque explicitamente cada um.

---

## Como Usar Esta Skill

Antes de gerar qualquer plano, leia os artefatos de entrada e as referências
de arquitetura. Nunca gere o plano com base em convenções genéricas de .NET —
sempre siga os padrões documentados neste projeto.

1. Leia os artefatos produzidos pelo comando `read-spec`:
   - `spec.md` — escopo, decisões funcionais, fora de escopo
   - `logic.md` — regras de negócio numeradas, estados e transições, fluxos por
     perfil, pontos de integração, casos de borda
2. Leia a skill `/architecture` por completo (`.claude/skills/architecture/SKILL.md`).
3. Leia o modelo do plano em `references/plan-template.md` (desta skill).
4. Execute o procedimento descrito na seção "Procedimento de execução" abaixo.

---

## Procedimento de execução

### Passo 1 — Extração dos artefatos da spec e do documento de lógica

Aplique as seções "Como ler a spec de produto", "Como ler o documento de lógica" e
"Como derivar impacto em módulos e aggregates" para produzir uma lista
intermediária (não publicada) com:

- Todos os aggregates (novos e existentes) tocados
- Todos os domain events novos
- Todos os SmartEnums novos — com os valores exatos listados no `logic.md`
- Todos os comportamentos (lado de escrita): cadeia completa por comportamento
- Todas as leituras (lado de leitura): cadeia completa por leitura
- Todos os emails/notificações: cadeia completa por email
- Todos os registros de infraestrutura: migrations, configs EF, registros de consumer, etc.
- Todos os eventos cross-module e a direção (módulo publicador → módulo consumidor)
- Todas as regras de negócio numeradas do `logic.md`, com a camada atribuída
- Todas as autorizações por perfil extraídas dos fluxos do `logic.md`
- Todos os casos de borda do `logic.md`, com o artefato de destino (Validator/Handler)

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

### Passo 3 — Montagem do plano (faseado por camada)

Com a lista de artefatos extraída e o estado atual verificado:

1. **Agrupe os artefatos por fase** conforme definido em `references/plan-template.md`:
   - Fase 0: Scaffold (apenas se módulo novo)
   - Fase 1: Domain
   - Fase 2: Shared
   - Fase 3: Application
   - Fase 4: Persistence
   - Fase 5: Infrastructure
   - Fase 6: Web
   Inclua apenas as fases que a funcionalidade efetivamente toca.

2. **Dentro de cada fase**, ordene os itens respeitando as dependências
   internas (ex.: SmartEnums antes de Domain Events, Domain Events antes de
   Aggregate). Use numeração **local por fase** — cada fase começa no `#1`.

3. **Para referências cross-fase**, use o formato `Fase X · #N`
   (ex.: `Depende de: Fase 1 · #3`).

4. **Valide o tamanho de cada fase** — se uma fase passar de ~12 itens,
   verifique se há artefatos que podem ser decompostos ou se algum item
   está classificado na camada errada.

5. **Amarre cada item à spec** — o campo `Spec-ref` deve apontar para a
   seção ou regra da spec que justifica a existência do artefato.

6. **Preencha a checklist de registro** — marque todos os pontos de
   bootstrap que serão tocados e descreva o que será adicionado em cada um.

7. **Liste riscos e pontos de atenção** — red flags encontrados na spec,
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

### Estrutura faseada

- O plano usa obrigatoriamente a estrutura de fases definida em `references/plan-template.md`
- Nenhum item de uma camada aparece em fase de camada diferente
  (ex.: um Handler não pode estar na Fase 1 — Domain)
- Cada fase termina com gate de build explícito
- Nenhuma fase tem mais de ~12 itens — se ultrapassar, revisar a decomposição

### Completude

- Todo comportamento (write) da spec tem a cadeia completa de write-side
- Toda leitura da spec tem a cadeia completa de leitura
- Todo email mencionado na spec tem os três artefatos do email pattern
- Todo aggregate novo tem os artefatos de bootstrap:
  EF StoreEventConfiguration + SnapshotConfiguration + Migration
- Todo domain event novo tem um `ConfigureEventReceiveEndpoint` correspondente
- Todo novo projection handler está listado para registro em `AddEventInteractors()`

### Consistência com as convenções

Valide contra as regras "O que NUNCA fazer" de cada camada em `/architecture`.
Em particular:

- Nenhum artefato do plano lê do PostgreSQL no lado de leitura
- Nenhum domain event transporta SmartEnum como tipo — sempre `string`
- Nenhum comando de email contém tipos não-primitivos
- Nenhum endpoint retorna entidade de domínio — sempre DTO de resposta
- Nenhum módulo referencia outro módulo diretamente — integração sempre via eventos
- Nenhuma validação de regra de negócio está atribuída ao Domain — sempre no Handler
- Toda rota segue o padrão `v{version:apiVersion}/{module}/...` sem `/api`

### Ordenação

- Todo item Domain aparece antes de qualquer item Application que o usa
- Todo item Shared aparece antes de qualquer item Application que o consome
- Toda Migration aparece antes do endpoint que depende da coluna/tabela nova
- Todo Domain Event aparece antes do Projection Handler que o consome
- Todo Projection Model aparece antes da Query que o lê
- Todo comando de email aparece antes do Handler de negócio que o enfileira
- Todo Consumer Registration aparece depois do Domain Event que configura

---

## Nunca fazer

- Gerar código — esta skill produz apenas o `plan.md`
- Assumir que artefatos existem sem verificar no repositório
- Omitir artefatos de infraestrutura (migrations, EF configs, consumer registrations)
- Misturar itens de módulos diferentes sem explicitar a fronteira cross-module
- Criar itens genéricos ("implementar validações") — cada item deve ser concreto
- Incluir refactors fora da spec sem listá-los em "Riscos e pontos de atenção"
- Ignorar ambiguidades da spec ou do logic doc — toda ambiguidade vira ponto de atenção
- Escrever o plano sem ter lido o estado atual do código
- Duplicar conteúdo da skill `/architecture` — referenciar
- Gerar o plano com base apenas no `spec.md` ignorando o `logic.md`
- Deixar qualquer regra de negócio numerada do `logic.md` sem artefato correspondente no plano
- Deixar qualquer caso de borda do `logic.md` sem destino no plano (Validator, Handler ou ponto de atenção)

## Sempre fazer

- Executar `read-spec` antes desta skill (se os artefatos não estiverem no contexto)
- Confirmar que tanto `spec.md` quanto `logic.md` estão no contexto antes de iniciar
- Ler a skill `/architecture` antes de gerar o plano
- Verificar o código existente antes de planejar cada artefato
- Seguir rigorosamente o formato de `references/plan-template.md`
- Declarar dependências explícitas entre itens (`Depende de: #N, #M`)
- Amarrar cada item a uma seção da spec ou do logic doc (`Spec-ref`)
- Preencher a checklist de registro por completo — o humano precisa ver que foram considerados
- Listar riscos e pontos de atenção — mesmo que não haja, escrever "Nenhum"
- Apresentar o plano e aguardar aprovação explícita antes de prosseguir
