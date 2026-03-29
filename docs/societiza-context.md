# SOCIETIZA — Contexto de Projeto para Claude

> **Como usar este arquivo:**
> Cole o conteúdo da seção relevante no início de cada conversa com o Claude (Chat, Cowork ou Code).
> Quanto mais específico for o contexto que você fornecer, menos tokens serão gastos em correções e retrabalho.

---

## 1. SYSTEM PROMPT — Cole isto em toda nova conversa

```
Você é um assistente especializado no projeto Societiza.

## Sobre o Produto
Societiza é um workflow societário SaaS (100% na nuvem, sem instalação) voltado para contabilidades que gerenciam abertura, alteração e encerramento de empresas no Brasil. Sede: Joinville, SC. Site: societiza.com.br

## Público-alvo
Contadores independentes, escritórios de contabilidade (pequenos e grandes), times de societário. Qualquer profissional que lide com processos societários no Brasil.

## Proposta de Valor
Organizar, automatizar e acompanhar todo o processo de abertura de empresas em um kanban societário 100% customizável. Tagline: "Seu societário. Seu controle."

## Funcionalidades Core
- Kanban customizável (colunas, etapas, status, templates prontos)
- Formulário para o cliente gerar card no kanban via link
- Checklist de tarefas + automações em portais de prefeitura (viabilidade, alvará etc.)
- Notificações automáticas por evento (cliente e contabilidade)
- Alertas inteligentes para processos parados
- Multi-estado nativo (adapta-se a cada prefeitura)
- Transparência para o cliente (acompanhamento em tempo real)

## Planos
- **Salinha** — Gratuito (1 usuário, até 3 processos ativos)
- **Escritório** — R$ 109,90/mês (até 3 usuários, processos ilimitados)
- **Enterprise** — Sob consulta (usuários ilimitados, gestão centralizada)

## Tom de Comunicação
Direto, profissional mas acessível. Fala com contadores que não têm tempo a perder. Evita jargão técnico desnecessário. Usa linguagem prática e orientada a resultado.

## Stack & Contexto Técnico
- Frontend: [preencha com sua stack — ex: Next.js, React, Tailwind]
- Backend: [preencha com sua stack — ex: Node.js, PostgreSQL, Prisma]
- Infra: [preencha — ex: Vercel, Railway, AWS]
- Repositório: [preencha o path/estrutura principal]

Sempre que eu não especificar o contexto, assuma que estamos trabalhando dentro do produto Societiza.
```

---

## 2. TEMPLATE POR FERRAMENTA

### 🗨️ Claude Chat — Tarefas pontuais e estratégicas

**Quando usar:** Copywriting, estratégia de produto, análise de feedbacks, rascunhos de e-mail, FAQ, documentação de features.

**Como iniciar a conversa:**

```
[Cole o System Prompt acima]

---

Tarefa: [descreva aqui o que você quer]

Contexto adicional: [cole prints, textos, dados ou exemplos relevantes]

Formato esperado: [ex: lista de bullet points / texto corrido / JSON / tabela]
```

**Exemplos de prompts eficientes:**

```
Escreva 3 variações de subject line para um e-mail de onboarding
do plano Salinha. O objetivo é que o contador faça o primeiro
cadastro de processo em até 24h após criar a conta.
```

```
Crie uma resposta para o seguinte feedback de cliente:
"O sistema é bom mas não consigo entender como configurar as colunas."
Tom: empático, prático, com link para documentação.
```

---

### 🖥️ Claude Cowork — Automações e fluxos de trabalho

**Quando usar:** Automatizar tarefas repetitivas, manipular arquivos, processar dados, rodar scripts, organizar pastas do projeto.

**Como iniciar:**

```
[Cole o System Prompt acima]

---

Objetivo: [o que precisa ser feito no computador/sistema]

Arquivos envolvidos: [paths ou anexe os arquivos]

Resultado esperado: [o que deve existir ao final]

Restrições: [ex: não apagar arquivos originais / manter padrão de nomenclatura X]
```

**Exemplos:**

```
Preciso organizar a pasta /exports com os CSVs de processos exportados
do sistema. Renomeie cada arquivo para o padrão:
AAAA-MM-DD_nome-do-escritorio.csv
```

```
Leia o arquivo feedback-clientes.txt e gere um relatório em markdown
com: top 5 problemas relatados, frequência de cada um, e sugestão
de solução para cada problema.
```

---

### 💻 Claude Code — Desenvolvimento do produto

**Quando usar:** Novas features, refatorações, revisão de código, debugging, migrations de banco, testes.

**Como iniciar (além do CLAUDE.md gerado pelo /init):**

```
[Cole o System Prompt acima — ou adicione ao CLAUDE.md do projeto]

---

Feature/Bug: [nome ou descrição curta]

Contexto técnico:
- Arquivo(s) envolvido(s): [paths]
- Comportamento atual: [o que acontece hoje]
- Comportamento esperado: [o que deve acontecer]

Restrições:
- [ex: não alterar a API pública deste módulo]
- [ex: manter compatibilidade com Node 18]

Testes esperados: [sim/não — descreva cenários se sim]
```

**Exemplos:**

```
Feature: Alerta de processo parado

Contexto técnico:
- Arquivo: src/services/alertService.ts
- Comportamento atual: nenhum alerta é disparado
- Comportamento esperado: se um processo ficar sem movimentação
  por X dias (configurável por escritório), o contador recebe
  e-mail com resumo dos processos parados

Restrições: usar o serviço de e-mail já existente em src/services/emailService.ts
Testes: sim — cenário com 0, 1 e N processos parados
```

---

## 3. ADIÇÕES AO CLAUDE.md (para Claude Code)

Adicione este bloco ao `CLAUDE.md` gerado pelo `/init` no repositório:

```markdown
## Contexto de Produto — Societiza

### O que é o Societiza
Workflow societário SaaS para contabilidades. Foco em abertura, alteração e encerramento de empresas no Brasil. Multi-estado, 100% na nuvem.

### Domínio do Negócio — Glossário
- **Processo**: Uma abertura/alteração/encerramento de empresa em andamento
- **Card**: Representação de um processo no Kanban
- **Etapa/Coluna**: Fase do workflow (ex: Viabilidade, Registro, Alvará)
- **Escritório**: Empresa de contabilidade cliente do Societiza
- **Operador/Usuário**: Contador que usa o sistema
- **Cliente final**: Empresário que está abrindo/alterando a empresa
- **Viabilidade**: Consulta prévia na prefeitura para verificar se CNAE é permitido no endereço
- **Alvará**: Licença de funcionamento emitida pela prefeitura

### Regras de Negócio Críticas
- Um processo sempre pertence a um escritório
- Um escritório pode ter múltiplos usuários (conforme plano)
- Processos ativos no plano Salinha: máximo 3
- Notificações devem ser disparadas por evento, nunca por polling
- Dados do cliente final nunca devem ser expostos para outros escritórios

### Padrões de Código do Projeto
- [preencha com seus padrões — ex: sempre usar tipos explícitos no TypeScript]
- [ex: funções de serviço não devem acessar req/res diretamente]
- [ex: toda query ao banco passa pelo repositório, nunca direto no controller]

### Comandos úteis
- [preencha com os comandos do seu projeto — ex: npm run dev, npm test, etc.]
```

---

## 4. REFERÊNCIA RÁPIDA — Diferenciais para usar nos prompts

Quando pedir textos ou decisões de produto, lembre o Claude destes diferenciais:

| Diferencial | Como usar |
|---|---|
| Kanban 100% customizável | "O sistema se adapta ao jeito de trabalhar de cada contabilidade, não o contrário" |
| Multi-estado nativo | "Funciona para qualquer prefeitura do Brasil, com checklists específicos por município" |
| Transparência para o cliente | "O cliente acompanha em tempo real e recebe e-mails automáticos — menos WhatsApp, mais confiança" |
| Alertas inteligentes | "O sistema avisa o contador quando um processo fica parado — nenhum cliente esquecido" |
| Plano gratuito permanente | "Plano Salinha gratuito para sempre — sem limite de tempo, sem cartão de crédito" |

---

## 5. CHECKLIST ANTES DE ENVIAR UM PROMPT

- [ ] O System Prompt do Societiza está colado no início da conversa?
- [ ] Descrevi o **comportamento esperado**, não só o problema?
- [ ] Especifiquei o **formato de saída** (texto, código, JSON, tabela...)?
- [ ] Para código: incluí os **paths dos arquivos** relevantes?
- [ ] Listei as **restrições** (o que não pode mudar)?

---

*Documento gerado em 25/03/2026 — baseado no contexto da landing page do Societiza.*
*Atualize a seção de Stack & Contexto Técnico com os dados reais do seu projeto.*