# Mudanças no Backend - Reestruturação do Draft/Publish de Workflow Template

Este documento descreve, em detalhes, as mudanças realizadas no backend da API Societiza durante a reestruturação do fluxo de workflow template.

O foco principal foi o bounded context de `WorkflowTemplate`, com uma mudança adicional de base na camada compartilhada de event store e no middleware global de tratamento de erros.

## 1. Objetivo Geral das Mudanças

As alterações no backend foram feitas para sustentar um fluxo de template mais próximo de um ciclo real de edição por rascunho e publicação, em vez de um editor CRUD direto.

Os objetivos principais foram:

- permitir que um template ativo seja clonado para um rascunho sem duplicar trabalho quando o rascunho já existir;
- suportar um fluxo de publicação que promova o rascunho de volta para o template ativo;
- preservar a ordem de etapas, campos de etapa, tarefas e campos de tarefa;
- aceitar tipos legados ou variações de nome vindas de versões anteriores do frontend ou de dados antigos;
- expor erros úteis da API em vez de transformar qualquer falha em `500 Internal Server Error`;
- manter domínio, handlers, projeções e contratos públicos coerentes com o novo comportamento;
- reduzir o vazio inicial da criação de template, já iniciando com uma primeira etapa.

Essas mudanças atravessaram várias camadas:

- event store compartilhado;
- aggregate e entidades do workflow-template;
- eventos de domínio;
- modelo de projeção e handlers de projeção;
- handlers e validators da camada de aplicação;
- contratos compartilhados de request/response;
- endpoints HTTP e rotas;
- middleware global de exceção;
- testes automatizados de domínio, aplicação e API.

## 2. Correção no Event Store: Persistência da Versão Correta do Evento

### Arquivo alterado

- [StoreEvent.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Core/Core.Domain/EventStore/StoreEvent.cs)

### O que mudou

A factory estática que cria o registro persistido do evento agora grava a versão do próprio evento de domínio, e não mais a versão final do aggregate.

Antes dessa correção, `StoreEvent.Create(...)` acabava serializando eventos com a versão errada quando um aggregate emitia mais de um evento dentro da mesma execução de comando. Isso quebrava operações que precisam persistir várias mudanças antes do commit terminar, especialmente:

- criação de draft;
- publicação de draft;
- fluxos de clone de template com várias entidades aninhadas;
- qualquer operação de aggregate que emita múltiplos eventos de step/task/field.

A nova implementação é simples e direta:

- o aggregate continua fornecendo identidade e contexto agregado;
- o evento continua sendo a fonte de verdade para versão e timestamp;
- os registros persistidos passam a guardar exatamente a versão atribuída no momento da criação do evento.

### Por que isso era importante

Essa mudança corrigiu uma das falhas de runtime mais relevantes do domínio de workflow template:

- persistências multi-evento estavam falhando;
- algumas operações viravam `500` porque o versionamento não batia no event store;
- os fluxos de draft e publish eram especialmente sensíveis, porque emitem uma cadeia de eventos no mesmo comando.

Na prática, isso estabilizou a escrita do backend o suficiente para o restante do trabalho de draft/publish funcionar corretamente.

## 3. Novo Ciclo de Draft para Workflow Templates

### Arquivos adicionados ou alterados

- [CreateWorkflowTemplateDraftHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Commands/Template/CreateWorkflowTemplateDraftHandler.cs)
- [CreateWorkflowTemplateDraftCommand.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Commands/Template/CreateWorkflowTemplateDraftCommand.cs)
- [CreateWorkflowTemplateDraftRequest.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Web/Endpoints/Modules/WorkflowTemplate/Template/Requests/Commands/CreateWorkflowTemplateDraftRequest.cs)
- [CreateWorkflowTemplateDraftEndpoint.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Web/Endpoints/Modules/WorkflowTemplate/Template/CreateWorkflowTemplateDraftEndpoint.cs)
- [CreateWorkflowTemplateDraftValidator.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Validators/Template/CreateWorkflowTemplateDraftValidator.cs)
- [CreateWorkflowTemplateDraftHandlerTests.cs](/Users/henilveira/Documents/societiza/api.societiza/tests/Web.Tests/Modules/WorkflowTemplate/Application/CreateWorkflowTemplateDraftHandlerTests.cs)

### O que o novo fluxo faz

A API agora suporta criar um rascunho a partir de um template existente.

O handler de comando:

- carrega o aggregate do template de origem;
- verifica se já existe um rascunho aberto vinculado a esse source template;
- devolve o rascunho existente se ele já existir;
- caso contrário, cria um novo aggregate de draft apontando `SourceTemplateId` para o template original;
- clona a estrutura do template de origem para o draft;
- persiste o draft pelo mesmo pipeline normal de append de eventos.

### Regra de idempotência

A criação de draft agora é idempotente por `SourceTemplateId`.

Isso significa:

- se o usuário clicar em "editar workflow" várias vezes;
- se o frontend repetir uma requisição;
- se a tela entrar e sair do modo de edição por navegação de rota;

o backend não cria um segundo draft ativo para o mesmo template de origem. Ele devolve o draft existente.

Isso evita:

- rascunhos duplicados;
- estados conflitantes de UI;
- edições aplicadas à cópia errada do workflow;
- vários drafts competindo entre si como fonte de verdade.

### Estratégia de clone

O clone foi escrito para preservar estrutura, mas evitando falhas por títulos duplicados dentro do aggregate.

O handler:

- percorre as etapas da origem em ordem;
- cria cada step no draft;
- copia os campos de etapa em ordem;
- copia as tarefas em ordem;
- copia os campos das tarefas em ordem;
- preserva descrições, configuração de tarefa, opções de campo e optionalidade;
- preserva a sequência original do workflow.

Como o aggregate ainda protege `AddStep` e `AddTask` contra títulos duplicados, o clone usa um título temporário único no momento da criação e depois atualiza a entidade de volta para o título original. Isso impede rejeições falsas de duplicidade durante o clone, sem perder a estrutura real.

### Por que isso era importante

Esse fluxo de criação de draft é a base da nova experiência:

- o template pode ser editado com segurança sem mutar o workflow ativo imediatamente;
- as mudanças ficam isoladas no draft;
- publicar deixa de ser uma edição direta destrutiva e passa a ser uma promoção controlada.

## 4. Fluxo de Publicação do Draft

### Arquivos adicionados ou alterados

- [PublishWorkflowTemplateHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Commands/Template/PublishWorkflowTemplateHandler.cs)
- [PublishWorkflowTemplateCommand.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Commands/Template/PublishWorkflowTemplateCommand.cs)
- [PublishWorkflowTemplateRequest.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Web/Endpoints/Modules/WorkflowTemplate/Template/Requests/Commands/PublishWorkflowTemplateRequest.cs)
- [PublishWorkflowTemplateEndpoint.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Web/Endpoints/Modules/WorkflowTemplate/Template/PublishWorkflowTemplateEndpoint.cs)
- [PublishWorkflowTemplateValidator.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Validators/Template/PublishWorkflowTemplateValidator.cs)
- [PublishWorkflowTemplateHandlerTests.cs](/Users/henilveira/Documents/societiza/api.societiza/tests/Web.Tests/Modules/WorkflowTemplate/Application/PublishWorkflowTemplateHandlerTests.cs)

### O que o publish faz

O handler de publicação agora suporta dois modos:

1. publicar um template já ativo quando a requisição aponta para um template que não é draft;
2. publicar um draft de volta para seu template de origem quando `SourceTemplateId` está presente.

No caminho draft -> source, o handler:

- carrega o aggregate do draft;
- carrega o aggregate do template de origem referenciado por `SourceTemplateId`;
- valida a estrutura do draft antes de aplicar;
- atualiza os metadados do template de origem com os dados do draft;
- reconstrói a estrutura do template de origem com base nas etapas, campos, tarefas e campos de tarefas ordenados do draft;
- ativa o template de origem se necessário;
- arquiva o draft após a publicação;
- persiste os eventos resultantes dos dois aggregates.

### Validação explícita de nomes duplicados antes de publicar

Uma das proteções mais importantes adicionadas neste ciclo foi a validação explícita de nomes duplicados no draft antes da publicação.

O handler de publish verifica:

- títulos de etapas duplicados no draft inteiro;
- títulos de tarefas duplicados dentro da mesma etapa.

Se houver duplicidade, o handler lança uma `ArgumentException` clara, explicando:

- o que está duplicado;
- onde a duplicidade acontece;
- que cada step ou task precisa ter nome único antes de publicar.

Isso foi necessário porque o aggregate continua impondo unicidade na criação/edição, e a publicação antes quebrava com erro opaco quando nomes duplicados chegavam ao backend.

### Comportamento atual e limite desta implementação

A implementação atual de publish reconstrói a estrutura do template de origem a partir do draft. Isso significa que o template ativo recebe o conteúdo do draft e o draft é arquivado ao final.

Isso resolve o ciclo de edição e protege a separação entre draft e ativo, mas ainda não é um motor completo de sincronização aditiva para instâncias em execução. O trabalho atual permanece dentro do bounded context de workflow-template e ainda não introduz um contexto separado de `WorkflowProcess` para processos em runtime.

Esse limite é importante:

- draft/publish de templates está implementado;
- sincronização de instâncias já em execução ainda é uma responsabilidade de domínio separada;
- a API agora expõe erros de publish de forma muito mais clara, mas ainda não propaga esse publish para um motor de processo vivo.

## 5. A Criação de Template Agora Inicia com a Primeira Etapa

### Arquivo alterado

- [CreateWorkflowTemplateHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Commands/Template/CreateWorkflowTemplateHandler.cs)

### O que mudou

Quando um novo workflow template é criado, ele não começa mais como um shell completamente vazio.

O handler agora:

- cria o aggregate do template;
- adiciona imediatamente uma etapa inicial chamada `Etapa 1`;
- fornece uma descrição padrão que orienta o usuário a definir processo, campos e tarefas nessa etapa;
- persiste o template já com esse primeiro ponto de edição presente.

### Por que isso era importante

Isso reduziu o atrito do estado vazio e alinhou o backend à UX pretendida:

- um novo draft já nasce utilizável;
- a primeira superfície de edição já existe sem obrigar o usuário a criar a primeira etapa manualmente;
- o draft pode funcionar como uma área de trabalho real, e não como um espaço em branco.

## 6. Suporte a Ordem em Steps, Step Fields, Tasks e Task Fields

### Arquivos alterados

Domínio:

- [WorkflowTemplateStep.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Entities/WorkflowTemplateStep.cs)
- [WorkflowTemplateStepTask.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Entities/WorkflowTemplateStepTask.cs)
- [TemplateStepField.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Entities/TemplateStepField.cs)
- [TemplateStepTaskField.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Entities/TemplateStepTaskField.cs)

Modelo de projeção:

- [ProjectionModel.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Projection/ProjectionModel.cs)

Eventos de domínio:

- [DomainEvents.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Events/DomainEvents.cs)

Handlers de aplicação:

- [AddTemplateStepFieldHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Commands/TemplateStepField/AddTemplateStepFieldHandler.cs)
- [UpdateTemplateStepFieldHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Commands/TemplateStepField/UpdateTemplateStepFieldHandler.cs)
- [AddWorkflowTemplateStepTaskHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Commands/TemplateStepTask/AddWorkflowTemplateStepTaskHandler.cs)
- [UpdateWorkflowTemplateStepTaskHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Commands/TemplateStepTask/UpdateWorkflowTemplateStepTaskHandler.cs)
- [AddTemplateStepTaskFieldHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Commands/TemplateStepTaskField/AddTemplateStepTaskFieldHandler.cs)
- [UpdateTemplateStepTaskFieldHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Commands/TemplateStepTaskField/UpdateTemplateStepTaskFieldHandler.cs)

Contratos compartilhados:

- [AddTemplateStepFieldCommand.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Commands/TemplateStepField/AddTemplateStepFieldCommand.cs)
- [UpdateTemplateStepFieldCommand.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Commands/TemplateStepField/UpdateTemplateStepFieldCommand.cs)
- [AddTemplateStepTaskFieldCommand.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Commands/TemplateStepTaskField/AddTemplateStepTaskFieldCommand.cs)
- [UpdateTemplateStepTaskFieldCommand.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Commands/TemplateStepTaskField/UpdateTemplateStepTaskFieldCommand.cs)

Respostas da API:

- [WorkflowTemplateResponse.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Response/Template/WorkflowTemplateResponse.cs)
- [WorkflowTemplateDetailResponse.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Response/Template/WorkflowTemplateDetailResponse.cs)
- [WorkflowTemplateStepFieldDetailResponse.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Response/TemplateStep/WorkflowTemplateStepFieldDetailResponse.cs)
- [WorkflowTemplateStepTaskFieldDetailResponse.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Response/TemplateStepTask/WorkflowTemplateStepTaskFieldDetailResponse.cs)

### O que mudou

O modelo de workflow template agora rastreia explicitamente um valor de `Order` em todos os pontos que precisam de sequenciamento estável e definido pelo usuário:

- etapas;
- campos de etapa;
- tarefas;
- campos de tarefa.

Essa ordem agora faz parte de:

- estado do aggregate;
- eventos de domínio emitidos;
- registros da projeção no read model;
- comandos usados nas operações de criação/edição;
- contratos de resposta consumidos pelo frontend;
- handlers de query usados para listar e inspecionar templates.

### Por que isso era importante

Antes disso, o sistema tinha uma noção de ordem muito mais fraca e dependia demais da ordem de inserção ou de suposições da interface. Isso dificultava:

- reordenar campos com clareza;
- preservar exatamente a sequência vista pelo usuário;
- fazer o frontend refletir o mesmo estado armazenado no backend;
- reconstruir de forma determinística a estrutura do draft ou workflow.

O suporte a order deixa o read model e o write model muito mais explícitos.

### Consequência prática

A API agora pode devolver campos e tarefas em ordem estável, e o frontend consegue reordenar e persistir essas entidades sem depender de ordem incidental de listas.

## 7. O Parser de Tipos de Field e Task Foi Fortalecido para Entradas Legadas

### Arquivo adicionado

- [WorkflowTemplateTypeParsers.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Enumerations/WorkflowTemplateTypeParsers.cs)

### O que esse utilitário faz

O backend agora tem um parser dedicado para tipos de field e task do workflow template.

Ele aceita:

- nomes canônicos dos enums;
- valores legados;
- entradas case-insensitive;
- grafias alternativas usadas por clientes antigos ou por migrações de dados anteriores.

### Comportamento de field type

Os tipos de field suportados incluem:

- `Text`
- `Boolean`
- `Integer`
- `Select`
- `Date`

O parser também aceita valores legados ou alternativos como:

- `text`
- `Texto`
- `selection`
- `selecao`
- `seleção`
- `bool`
- `int`
- `number`
- `numero`
- `data`

### Comportamento de task type

Os tipos de task suportados incluem:

- `Manual`
- `Automatic`
- `Approval`

O parser também aceita valores legados ou alternativos como:

- `form`
- `checklist`
- `document`
- `documento`
- `auto`
- `automático`
- `automatico`
- `aprovacao`
- `aprovação`

### Por que isso era importante

Isso protegeu a API de falhar quando o frontend ou dados já persistidos enviavam valores que não batiam exatamente com o formato atual dos enums.

Também reduziu a chance de o usuário encontrar um erro genérico de backend enquanto edita um template que contenha definições antigas de fields ou tasks.

## 8. Mudanças no Modelo de Projeção e nas Queries

### Arquivos alterados

- [ProjectionWorkflowTemplateEventHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Events/ProjectionWorkflowTemplateEventHandler.cs)
- [ProjectionTemplateStepFieldEventHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Events/ProjectionTemplateStepFieldEventHandler.cs)
- [ProjectionTemplateStepTaskFieldEventHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Events/ProjectionTemplateStepTaskFieldEventHandler.cs)
- [GetAllWorkflowTemplatesHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Queries/Template/GetAllWorkflowTemplatesHandler.cs)
- [GetWorkflowTemplateDetailByIdHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Queries/Template/GetWorkflowTemplateDetailByIdHandler.cs)

### O que mudou na projeção

O read model agora armazena e projeta a estrutura nova com mais precisão:

- os metadados do template agora incluem vínculo de origem e estado de draft;
- os registros de step/task/field agora incluem order;
- os handlers de projeção atualizam o read model com payloads de evento mais ricos;
- as queries de detail e list agora devolvem os dados necessários para um editor consciente de draft e de ordem.

### O que mudou nas queries

Os handlers de query agora retornam metadados mais ricos do template:

- `Status`
- `Version`
- `SourceTemplateId`
- `IsDerivedDraft`
- etapas ordenadas
- campos ordenados
- tarefas ordenadas

Isso é importante porque o frontend não precisa mais inferir o estado do editor a partir de uma resposta mínima. A API agora informa que tipo de template é aquele, de onde ele veio e como ele deve ser renderizado.

## 9. Os Contratos Públicos Foram Ampliados para Suportar Draft/Publish e Ordem

### Arquivos alterados

- [CreateWorkflowTemplateCommand.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Commands/Template/CreateWorkflowTemplateCommand.cs)
- [WorkflowTemplateResponse.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Response/Template/WorkflowTemplateResponse.cs)
- [WorkflowTemplateDetailResponse.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Response/Template/WorkflowTemplateDetailResponse.cs)
- [WorkflowTemplateStepFieldDetailResponse.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Response/TemplateStep/WorkflowTemplateStepFieldDetailResponse.cs)
- [WorkflowTemplateStepTaskFieldDetailResponse.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Response/TemplateStepTask/WorkflowTemplateStepTaskFieldDetailResponse.cs)
- [AddTemplateStepFieldCommand.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Commands/TemplateStepField/AddTemplateStepFieldCommand.cs)
- [UpdateTemplateStepFieldCommand.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Commands/TemplateStepField/UpdateTemplateStepFieldCommand.cs)
- [AddTemplateStepTaskFieldCommand.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Commands/TemplateStepTaskField/AddTemplateStepTaskFieldCommand.cs)
- [UpdateTemplateStepTaskFieldCommand.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Shared/Commands/TemplateStepTaskField/UpdateTemplateStepTaskFieldCommand.cs)

### O que mudou

Os contratos públicos foram expandidos para refletir melhor os requisitos de lifecycle e ordenação.

As adições mais importantes incluem:

- `SourceTemplateId` para rastrear de qual template um draft foi derivado;
- `IsDerivedDraft` para diferenciar template normal de cópia de rascunho;
- `Version` nas respostas de leitura;
- `Order` nos comandos e respostas de fields;
- suporte mais consistente a renderização ordenada nas respostas detalhadas.

### Por que isso era importante

Esses campos não são cosméticos. Eles fazem parte do contrato necessário para:

- separar estado de UI entre template ativo e cópia em rascunho;
- permitir reordenação previsível;
- mostrar a ação correta de publicar/editar no frontend;
- reduzir suposições do lado do cliente.

## 10. Os Endpoints HTTP e as Rotas Foram Extendidos

### Arquivos alterados

- [WorkflowTemplateRoutes.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Web/Endpoints/Routes/WorkflowTemplateRoutes.cs)
- [AddTemplateStepFieldEndpoint.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Web/Endpoints/Modules/WorkflowTemplate/TemplateStepField/AddTemplateStepFieldEndpoint.cs)
- [UpdateTemplateStepFieldEndpoint.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Web/Endpoints/Modules/WorkflowTemplate/TemplateStepField/UpdateTemplateStepFieldEndpoint.cs)
- [AddTemplateStepTaskFieldEndpoint.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Web/Endpoints/Modules/WorkflowTemplate/TemplateStepTaskField/AddTemplateStepTaskFieldEndpoint.cs)
- [UpdateTemplateStepTaskFieldEndpoint.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Web/Endpoints/Modules/WorkflowTemplate/TemplateStepTaskField/UpdateTemplateStepTaskFieldEndpoint.cs)
- [CreateWorkflowTemplateDraftEndpoint.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Web/Endpoints/Modules/WorkflowTemplate/Template/CreateWorkflowTemplateDraftEndpoint.cs)
- [PublishWorkflowTemplateEndpoint.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Web/Endpoints/Modules/WorkflowTemplate/Template/PublishWorkflowTemplateEndpoint.cs)

### O que mudou

A camada HTTP agora expõe as novas operações de draft e publish, e os endpoints de step/task/field foram atualizados para refletir os novos payloads dos comandos.

Isso significa que a API agora suporta:

- criar um draft a partir de um template de origem;
- publicar um draft de volta para seu template de origem;
- adicionar e atualizar campos de etapa com order explícito;
- adicionar e atualizar campos de tarefa com order explícito.

### Por que isso era importante

A camada de rotas é a entrada pública de toda a feature. Sem isso, o domínio e a aplicação existiriam, mas ficariam inacessíveis pela API.

## 11. O Tratamento de Erros Foi Melhorado para Retornar Respostas Estruturadas

### Arquivo alterado

- [ExceptionHandlingMiddleware.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Web/Middlewares/ExceptionHandlingMiddleware.cs)

### O que mudou

O middleware global de exceção não trata mais toda falha da mesma forma.

Ele agora mapeia tipos conhecidos de exceção para respostas HTTP apropriadas:

- `ValidationException` -> `400 Bad Request`
- `DuplicateEntityException` -> `409 Conflict`
- `AggregateIsDeletedException` -> `409 Conflict`
- `ArgumentException` -> `400 Bad Request`
- `KeyNotFoundException` -> `404 Not Found`
- `UnauthorizedAccessException` -> `401 Unauthorized`
- fallback -> `500 Internal Server Error`

### Formato da resposta

O middleware agora retorna um payload JSON estruturado contendo:

- `statusCode`
- `message`
- `path`
- `traceId`
- `errors` para falhas de validação
- `detail`

### Por que isso era importante

Essa foi uma das melhorias operacionais mais úteis do backend.

Antes, muitas falhas de domínio e aplicação eram convertidas em erro interno genérico, o que tornava a depuração pela UI ou pelos logs do Docker desnecessariamente difícil.

Agora a API dá informação suficiente para entender:

- onde a falha aconteceu;
- qual foi o tipo da falha;
- se o problema é validação, conflito, aggregate ausente ou exceção inesperada;
- qual `traceId` usar para correlacionar logs.

## 12. Os Validators Foram Ajustados ao Novo Contrato

### Arquivos alterados

- [AddTemplateStepFieldValidator.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Validators/TemplateStepField/AddTemplateStepFieldValidator.cs)
- [UpdateTemplateStepFieldValidator.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Validators/TemplateStepField/UpdateTemplateStepFieldValidator.cs)
- [AddWorkflowTemplateStepTaskValidator.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Validators/TemplateStepTask/AddWorkflowTemplateStepTaskValidator.cs)
- [UpdateWorkflowTemplateStepTaskValidator.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Validators/TemplateStepTask/UpdateWorkflowTemplateStepTaskValidator.cs)
- [AddTemplateStepTaskFieldValidator.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Validators/TemplateStepTaskField/AddTemplateStepTaskFieldValidator.cs)
- [UpdateTemplateStepTaskFieldValidator.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Validators/TemplateStepTaskField/UpdateTemplateStepTaskFieldValidator.cs)
- [CreateWorkflowTemplateDraftValidator.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Validators/Template/CreateWorkflowTemplateDraftValidator.cs)
- [PublishWorkflowTemplateValidator.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Validators/Template/PublishWorkflowTemplateValidator.cs)

### O que mudou

Os validators foram alinhados ao novo comportamento da aplicação e ao novo formato de dados.

Eles agora cobrem:

- os comandos de criação e publicação de draft;
- os payloads atualizados de fields com order;
- os comandos de step/task que passaram a trabalhar com a estrutura de ordenação e lifecycle de draft;
- o caminho de publish que é sensível à estrutura do draft.

### Por que isso era importante

Validators fazem parte do contrato público da camada de aplicação. Assim que os comandos mudam, os validators precisam mudar também, senão requisições válidas passam a ser rejeitadas ou requisições inválidas continuam chegando ao domínio.

## 13. Testes Foram Adicionados e Atualizados para Proteger o Novo Comportamento

### Arquivos adicionados ou alterados

Testes de aplicação e domínio:

- [CreateWorkflowTemplateDraftHandlerTests.cs](/Users/henilveira/Documents/societiza/api.societiza/tests/Web.Tests/Modules/WorkflowTemplate/Application/CreateWorkflowTemplateDraftHandlerTests.cs)
- [PublishWorkflowTemplateHandlerTests.cs](/Users/henilveira/Documents/societiza/api.societiza/tests/Web.Tests/Modules/WorkflowTemplate/Application/PublishWorkflowTemplateHandlerTests.cs)
- [WorkflowTemplateFieldBehaviorTests.cs](/Users/henilveira/Documents/societiza/api.societiza/tests/Web.Tests/Modules/WorkflowTemplate/Domain/WorkflowTemplateFieldBehaviorTests.cs)
- [WorkflowTemplateTypeParsersTests.cs](/Users/henilveira/Documents/societiza/api.societiza/tests/Web.Tests/Modules/WorkflowTemplate/Domain/WorkflowTemplateTypeParsersTests.cs)
- [WorkflowTemplateAggregateTests.cs](/Users/henilveira/Documents/societiza/api.societiza/tests/Web.Tests/Modules/WorkflowTemplate/Domain/Aggregates/WorkflowTemplateAggregateTests.cs)

Testes existentes atualizados para refletir os novos contratos e comportamentos:

- handlers de step field;
- handlers de task field;
- handlers de projeção;
- handlers de query de template;
- testes do aggregate.

### O que os testes cobrem

A suíte atualizada verifica:

- criação de draft para um template de origem;
- idempotência na criação quando o draft já existe;
- publish com estrutura válida;
- falha clara de publish quando há títulos duplicados de step ou task;
- operações de step field e task field respeitando order;
- handlers de projeção materializando o estado correto;
- parser aceitando a superfície expandida de entradas legadas;
- comportamento do aggregate compatível com clone e ordenação.

### Resultado de validação

A suíte backend foi executada dentro de um container Docker com SDK .NET e passou com sucesso:

- `179` testes aprovados
- `0` falhas
- `0` ignorados

Essa execução foi importante porque validou o backend em um ambiente próximo ao de produção, e não só por inspeção de código.

## 14. Resumo da Estrutura de Dados que o Backend Agora Guarda e Retorna

Depois dessas mudanças, o modelo de template ficou mais rico e mais explícito.

### Metadados do workflow template

Os templates agora expõem:

- `Id`
- `Name`
- `Description`
- `Version`
- `Status`
- `CreatedAt`
- `UpdatedAt`
- `SourceTemplateId`
- `IsDerivedDraft`

### Metadados de etapa

As etapas agora expõem:

- `Id`
- `TemplateId`
- `Title`
- `Description`
- `Order`
- timestamps de criação e atualização

### Metadados de tarefa

As tarefas agora expõem:

- `Id`
- `TemplateId`
- `StepId`
- `Title`
- `Description`
- `Type`
- `Configuration`
- `Order`
- `IsOptional`
- timestamps de criação e atualização

### Metadados de field de etapa

Os fields de etapa agora expõem:

- `Id`
- `TemplateId`
- `StepId`
- `Label`
- `FieldType`
- `Options`
- `Order`
- timestamps de criação e atualização

### Metadados de field de tarefa

Os fields de tarefa agora expõem:

- `Id`
- `TemplateId`
- `StepId`
- `TaskId`
- `Label`
- `FieldType`
- `Options`
- `Order`
- timestamps de criação e atualização

## 15. Impacto Arquitetural

O maior efeito arquitetural dessas mudanças é que workflow templates passaram a ser tratados como estruturas versionadas e editáveis, e não mais como um objeto flat de edição imediata.

O backend agora suporta um ciclo mais robusto:

1. criar um template;
2. iniciar com uma etapa base;
3. abrir um draft derivado do template ativo;
4. editar a estrutura do draft;
5. validar o draft antes de publicar;
6. publicar o draft de volta para o template ativo;
7. arquivar o draft;
8. expor erros estruturados quando algo dá errado.

Esse ciclo está muito mais próximo do modelo de produto esperado do que o fluxo anterior de edição direta.

## 16. Limites Conhecidos e O Que Não Foi Resolvido Aqui

Para manter este documento preciso, é importante registrar o que essas mudanças não implementam ainda:

- ainda não existe um módulo dedicado de `WorkflowProcess` no backend;
- o publish reconstrói o template de origem a partir do draft, mas ainda não é um motor completo de sincronização aditiva para instâncias já em execução;
- o backend agora expõe erros muito melhores, mas qualquer `500` remanescente de outras áreas ainda precisa do seu próprio tratamento de domínio.

Esses limites não são falhas deste trabalho; são apenas o escopo atual do backend.

## 17. Arquivos Mais Importantes para Ler Primeiro

Se você quiser entender rapidamente as mudanças no backend, comece por estes arquivos:

1. [StoreEvent.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Core/Core.Domain/EventStore/StoreEvent.cs)
2. [CreateWorkflowTemplateDraftHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Commands/Template/CreateWorkflowTemplateDraftHandler.cs)
3. [PublishWorkflowTemplateHandler.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Application/UseCases/Commands/Template/PublishWorkflowTemplateHandler.cs)
4. [WorkflowTemplate.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Aggregates/WorkflowTemplate.cs)
5. [DomainEvents.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Events/DomainEvents.cs)
6. [ProjectionModel.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Projection/ProjectionModel.cs)
7. [WorkflowTemplateTypeParsers.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Modules/WorkflowTemplate/WorkflowTemplate.Domain/Enumerations/WorkflowTemplateTypeParsers.cs)
8. [ExceptionHandlingMiddleware.cs](/Users/henilveira/Documents/societiza/api.societiza/src/Web/Middlewares/ExceptionHandlingMiddleware.cs)

Esses arquivos mostram o núcleo das mudanças:

- correção de persistência;
- lifecycle de draft;
- lifecycle de publish;
- estrutura de domínio;
- contrato de API;
- diagnóstico de runtime.

