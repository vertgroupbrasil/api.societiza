# WorkflowTemplate - Contexto Atual do MVP

> Atualizado em 2026-04-02.
> Este arquivo consolida as decisoes atuais de negocio do WorkflowTemplate apos revisao do codigo e esclarecimentos do usuario.
> Quando este arquivo conflitar com suposicoes antigas em `docs/domain-model.md`, prefira este arquivo para o trabalho do MVP.

---

## 1. Papel do modulo

- O modulo gerencia templates de workflow usados para configurar fluxos de processos societarios.
- Um template e composto por etapas, tarefas e campos.
- Um template sempre e criado vazio primeiro.
- Etapas, tarefas e campos sao configurados depois, por endpoints separados.

---

## 2. Quem pode criar e administrar templates

- Neste momento, apenas usuarios `System Admin` criam templates base.
- A contabilidade nao pode criar um template do zero nesta fase.
- A contabilidade nao pode usar um template existente como base para criar outro template nesta fase.
- As acoes de ativar e arquivar sao manuais e executadas por `System Admin`.

---

## 3. Regra de ativacao

- Um template pode ser criado vazio.
- A estrutura minima so e validada quando o usuario tenta ativar o template.
- Para ativar um template, deve existir pelo menos:
  - uma etapa configurada
  - e essa etapa deve conter pelo menos uma tarefa
- O sistema nao deve exigir a estrutura completa durante a criacao do template, apenas no momento da ativacao.

---

## 4. Ciclo de status aprovado

- Status permitidos no MVP:
  - `Draft`
  - `Active`
  - `Archived`
- Transicoes permitidas no MVP:
  - `Draft -> Active`
  - `Draft -> Archived`
  - `Active -> Archived`
- Nao aprovado neste momento:
  - `Archived -> Active`
  - `Active -> Draft`
- Mesmo sendo uma acao manual do `System Admin`, o backend deve validar a matriz de transicoes acima.

---

## 5. Simplificacoes aprovadas para o MVP

- Nao existe versionamento de template neste momento.
- `TemplateVersion` nao deve ser tratado como regra de negocio ativa no MVP.
- Todas as tarefas sao manuais neste momento.
- Os comportamentos de tarefa `Automatic` e `Approval` estao fora do escopo do MVP.
- O backend deve tratar `Manual` como o tipo funcional valido nesta fase.
- As opcoes de campos `Select` ja estao mapeadas e devem ser validadas no backend.
- Se etapas irmas ou tarefas irmas receberem valores conflitantes de `Order`, o sistema deve reordenar automaticamente.
- Se a API tentar atualizar ou remover uma etapa, tarefa ou campo inexistente, deve retornar `404`.

---

## 6. Fora do escopo por enquanto

- Criacao de templates pela contabilidade
- Criacao de um novo template a partir de um template existente
- Qualquer fluxo de "template derivado de template base"
- Historico de versoes de template
- Regras de isolamento por tenant dentro deste modulo

Observacao:
- O isolamento de dados esta sendo tratado separadamente e nao deve ser assumido como resolvido por este modulo neste momento.

---

## 7. Leitura tecnica do codigo atual

- O modulo atual ja possui:
  - um agregado `WorkflowTemplate`
  - comandos e endpoints separados para template, etapa, tarefa, campo de etapa e campo de tarefa
  - escritas baseadas em event sourcing
  - leituras baseadas em projecoes
- O codigo atual ainda contem `TemplateVersion`, mas isso nao representa mais o comportamento aprovado para o MVP.
- O codigo atual ainda contem multiplos valores de `TaskType`, mas a regra do MVP deve ser tratada como `Manual` apenas.
- O codigo atual provavelmente ainda nao aplica a regra de ativacao que exige pelo menos uma etapa com pelo menos uma tarefa.
- O codigo atual provavelmente ainda nao aplica a matriz de transicoes de status aprovada.
- O codigo atual provavelmente ainda nao retorna `404` de forma consistente para recursos filhos inexistentes.
- Existe um provavel gap de projecao/event-bus na propagacao de remocao de etapas e tarefas, que deve ser revisado antes de confiar totalmente na consistencia das leituras.

---

## 8. Orientacao para proximos chats

- Use este documento como contexto principal de trabalho para as decisoes de MVP do WorkflowTemplate.
- Prefira simplificacao de MVP em vez de expandir arquitetura.
- Nao assuma clonagem, derivacao ou versionamento enquanto esse escopo nao for reaberto.
- Nao assuma que a contabilidade pode criar ou derivar templates na fase atual.
- Se as regras de negocio no codigo conflitarem com este arquivo, a proxima implementacao deve aproximar o codigo deste arquivo.
