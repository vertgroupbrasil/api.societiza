# Roadmap — Societiza

> Atualizado em 31/03/2026.
> Referência canônica de prioridades de desenvolvimento. Manter atualizado a cada tarefa concluída.

---

## Fase 1 — Fundação

- [x] **Tarefa 1** — Migrar todas as enumerations para SmartEnum (Accountancy e WorkflowTemplate)
- [x] **Tarefa 2** — Hardening do `ExpireInvitesJob` (paralelismo + log de summary + idempotência)
- [x] **Tarefa 3** — Limpeza geral do projeto (`IJwtTokenService`, `GenerateRandomPassword`, código morto, NuGet)
- [ ] **Tarefa 4** — Mapeamento de autenticação e permissões (JWT, refresh token, autorização por `UserRole`)

---

## Fase 2 — Identity completo

- [ ] **Tarefa 5** — Fluxo de convite de usuário end-to-end (envio, aceite, reenvio, revogação)
- [ ] **Tarefa 6** — Gerenciamento de usuários (listagem, edição, desativação por role)
- [ ] **Tarefa 7** — Isolamento de dados por `AccountancyId` (filtro global em queries e commands)

---

## Fase 3 — Core do produto

- [ ] **Tarefa 8** — Disponibilização de templates por Accountancy (escopo global vs. por escritório)
- [ ] **Tarefa 9** — Estruturar `WorkflowProcess` (novo módulo: domínio, eventos, projeções, kanban)

---

## Fora de prioridade

- [ ] Implementação correta de tarefas automatizadas (`TaskType.Automatic`) — depende da Tarefa 9

---

## Prompts gerados

| Tarefa | Arquivo |
|---|---|
| Fase 1 — Tarefas 1, 2 e 3 | `docs/prompts/prompt-fase1-completo.md` |

---

## Referências

- Modelo de domínio → `docs/domain-model.md`
- Contexto do produto → `docs/societiza-context.md`
- Skills de arquitetura → `.claude/skills/architecture/SKILL.md`
- Skills de infraestrutura → `.claude/skills/infrastructure/SKILL.md`