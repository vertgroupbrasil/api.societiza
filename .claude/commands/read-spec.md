---
name: read-spec
description: >
  Busca a spec ativa (spec.md) no repositório product.societiza via GitHub MCP.
  Descobre features com status approved ou in-progress no frontmatter YAML de
  cada spec. Entrega o conteúdo ao contexto do agente.
allowed-tools: mcp__github__get_file_contents
argument-hint: "[feature-name]"
---

# read-spec

## Propósito

Buscar a spec de produto ativa — `spec.md` — do repositório
`vertgroupbrasil/product.societiza` e disponibilizá-la no contexto do agente.
Este command apenas faz fetch — não interpreta, não planeja, não executa.

Se `$ARGUMENTS` for fornecido, use-o como nome da feature diretamente
(pule o passo 1). Caso contrário, descubra a feature ativa.

---

## Passos

### 1. Identificar a feature ativa (apenas se `$ARGUMENTS` estiver vazio)

Use a tool `mcp__github__get_file_contents` para listar o diretório:

```
repo:    vertgroupbrasil/product.societiza
branch:  main
path:    specs/
```

Para cada subdiretório encontrado, leia o arquivo `specs/{feature}/spec.md`
usando `mcp__github__get_file_contents`. Extraia o frontmatter YAML (bloco
entre `---`) e verifique o campo `status`.

Selecione a(s) feature(s) cujo status seja `approved` ou `in-progress`.

**Se nenhuma feature tiver status `approved` ou `in-progress`**, pare e
pergunte ao humano qual feature planejar. Não prossiga sem uma feature
definida.

**Se mais de uma feature estiver ativa**, liste todas e pergunte ao humano
qual delas usar. Não prossiga sem confirmação.

### 2. Buscar o artefato da spec

Com o nome da feature (`{feature}`) definido, use
`mcp__github__get_file_contents` para buscar o arquivo abaixo no
branch `main` do repositório `vertgroupbrasil/product.societiza`:

| Artefato | Caminho                   |
|----------|---------------------------|
| Spec     | `specs/{feature}/spec.md` |

**Se o arquivo não existir**, pare imediatamente e exiba:

```
SPEC NÃO ENCONTRADA
Feature:  {feature}
Esperado: specs/{feature}/spec.md
Repo:     vertgroupbrasil/product.societiza @main
Ação:     garanta que a spec existe e tem status approved ou in-progress.
```

Não prossiga ao passo 3 se o artefato estiver faltando.

### 3. Entregar o conteúdo

Após buscar o arquivo com sucesso, disponibilize no contexto:

- **Feature**: nome da feature identificada
- **spec.md**: conteúdo completo

O command termina aqui. O agente decide o próximo passo com base no
conteúdo entregue.