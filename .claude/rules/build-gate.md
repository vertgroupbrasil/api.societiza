---
name: build-gate
description: >
  Regra sempre ativa. Nenhuma fase de implementação avança sem build limpo
  da fase anterior. Aplica-se a qualquer tarefa que escreva código .cs ou .csproj.
---

# Build Gate

O plano técnico de qualquer feature é dividido em fases por camada. Esta regra
define o comportamento obrigatório entre fases.

## Regra

**Nunca inicie a próxima fase sem rodar `dotnet build Societiza.sln` e obter
zero errors na fase atual.**

## Por que existe

Erros de compilação acumulam silenciosamente quando o agente implementa
múltiplas camadas sem validar. Um erro no Domain que poderia ter sido
detectado em 5 minutos se propaga para Application, Persistence e Web —
tornando o diagnóstico muito mais difícil e o retrabalho muito maior.

## Como aplicar

1. Ao terminar todos os itens de uma fase, rode:
   ```bash
   dotnet build Societiza.sln
   ```
2. Se houver errors: corrija na fase atual antes de avançar. Não acumule
   erros para "resolver depois".
3. Se houver apenas warnings sobre código próprio (não de dependências): avalie
   se são consequência de artefatos que serão criados na próxima fase. Se sim,
   pode avançar — mas documente no resumo da fase.
4. Só após build limpo (zero errors): informe o humano que a fase foi concluída
   e inicie a próxima.

## Exceção: Fase 0 — Scaffold

A fase de scaffold cria arquivos `.csproj` e a estrutura de pastas, mas pode
não ter código suficiente para compilar isoladamente (ex.: `AssemblyReference.cs`
vazio). Neste caso, rode o build apenas para confirmar que a solução carrega
sem erros de referência — não é necessário que os projetos novos tenham
implementação completa ainda.