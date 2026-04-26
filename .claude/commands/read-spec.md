---
name: read-spec
description: >
  Busca spec.md e logic.md da funcionalidade ativa no repositório product.societiza
  via GitHub MCP. Ambos devem existir com `status: approved` — qualquer um faltando
  ou não aprovado bloqueia o comando. Entrega os dois ao contexto do agente.
allowed-tools: mcp__github__get_file_contents
argument-hint: "[feature-name]"
---

# read-spec

## Propósito

Buscar `spec.md` e `logic.md` do repositório `vertgroupbrasil/product.societiza`
e disponibilizá-los no contexto do agente. **Ambos são obrigatórios e devem ter
`status: approved`.** Se qualquer um estiver ausente ou fora desse status, o
comando para imediatamente — o plano técnico não pode ser gerado sem os dois.

Este comando apenas faz busca — não interpreta, não planeja, não executa.

Se `$ARGUMENTS` for fornecido, use-o como nome da funcionalidade diretamente
(pule o passo 1). Caso contrário, descubra a funcionalidade ativa.

---

## Passos

### 1. Identificar a funcionalidade ativa (apenas se `$ARGUMENTS` estiver vazio)

Use a ferramenta `mcp__github__get_file_contents` para listar o diretório:

```
repo:    vertgroupbrasil/product.societiza
branch:  main
path:    specs/
```

Para cada subdiretório encontrado, leia `specs/{feature}/spec.md` e extraia
o frontmatter YAML. Selecione funcionalidades com `status: approved`.

**Se nenhuma tiver `status: approved`**, pare e pergunte ao humano qual funcionalidade
usar. **Se mais de uma estiver aprovada**, liste e pergunte. Não prossiga sem
uma funcionalidade definida.

### 2. Buscar e validar os dois artefatos

Com a funcionalidade definida, busque **ambos** no branch `main` de
`vertgroupbrasil/product.societiza`:

| Artefato | Caminho                    |
|----------|----------------------------|
| Spec     | `specs/{feature}/spec.md`  |
| Logic    | `specs/{feature}/logic.md` |

Para cada artefato, aplique a mesma verificação:

**Se o arquivo não existir ou `status` não for `approved`**, pare e exiba:

```
BLOQUEADO — spec técnica não pode ser gerada
Feature:  {feature}
Problema: {arquivo} — {não encontrado | status: {status atual}}
Ação:     ambos spec.md e logic.md precisam existir com status approved
          no repositório product.societiza antes de prosseguir.
```

Não prossiga ao passo 3 se qualquer artefato falhar na verificação.

### 3. Entregar o conteúdo

Após ambos os arquivos passarem na verificação, disponibilize no contexto:

- **Funcionalidade**: nome da funcionalidade identificada
- **spec.md**: conteúdo completo
- **logic.md**: conteúdo completo

O comando termina aqui. O agente decide o próximo passo com base no
conteúdo entregue.