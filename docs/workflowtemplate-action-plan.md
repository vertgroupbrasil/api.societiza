# WorkflowTemplate - Plano de Acao MVP

> Baseado em `docs/workflowtemplate-current-context.md`.
> Objetivo: listar apenas as acoes principais para o modulo rodar conforme o contexto aprovado.

## 1. Regras de ciclo de vida

- Validar a ativacao do template no backend.
- Permitir criacao de template vazio.
- Exigir na ativacao:
  - pelo menos uma etapa
  - e pelo menos uma tarefa dentro de alguma etapa
- Validar a matriz de transicao de status:
  - `Draft -> Active`
  - `Draft -> Archived`
  - `Active -> Archived`
- Bloquear:
  - `Archived -> Active`
  - `Active -> Draft`

## 2. Simplificacao do MVP

- Tratar `Manual` como unico tipo funcional de tarefa no MVP.
- Nao considerar `Automatic` e `Approval` como comportamentos ativos neste fluxo.
- Tratar `TemplateVersion` apenas como legado tecnico, sem regra de negocio ativa.
- Nao expandir o modulo para clonagem, derivacao ou versionamento.

## 3. Estrutura do template

- Manter criacao do template separada da configuracao de etapas, tarefas e campos.
- Garantir que a validacao estrutural aconteca na ativacao, e nao na criacao.
- Validar `Options` para campos `Select`.
- Definir regra clara:
  - `Options` obrigatorio para `Select`
  - `Options` nao obrigatorio para os demais tipos

## 4. Ordenacao

- Implementar reordenacao automatica de etapas irmas quando houver conflito de `Order`.
- Implementar reordenacao automatica de tarefas irmas quando houver conflito de `Order`.
- Garantir que a ordem final persistida fique consistente apos inclusao e atualizacao.

## 5. Contrato de comportamento

- Retornar `404` ao tentar atualizar ou remover etapa inexistente.
- Retornar `404` ao tentar atualizar ou remover tarefa inexistente.
- Retornar `404` ao tentar atualizar ou remover campo inexistente.
- Ajustar comandos, handlers e validacoes para refletir exatamente essas regras.

## 6. Fechamento de alinhamento

- Revisar o codigo do modulo para remover comportamentos que contrariem o contexto atual.
- Atualizar a documentacao antiga do modulo quando ela conflitar com o contexto atual do MVP.
- Usar `docs/workflowtemplate-current-context.md` como referencia principal para as proximas implementacoes.
