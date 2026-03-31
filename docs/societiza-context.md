# SOCIETIZA — Project Context

> Reference document about the Societiza product: what it is, who it's for, how it works and how it was built.
> Updated on 31/03/2026.
> For Claude: attach to the Project in Chat. For humans: onboarding reading and context alignment.

---

## 1. PRODUCT

### What it is

Societiza is a **corporate workflow SaaS** built for accounting firms that manage company formation, amendment, and dissolution in Brazil. Fully cloud-based, no installation required.

- **Headquarters:** Joinville, SC
- **Website:** societiza.com.br
- **Tagline:** "Brazil's largest corporate infrastructure."

### Problem it solves

Accounting firms that handle company formation manage dozens of parallel processes — each with multiple stages, deadlines, municipal portal requirements, and different documents. Without a dedicated tool, control falls back to spreadsheets, WhatsApp, and memory. Processes stall, clients go unanswered, and the firm loses productivity.

### Value proposition

A customizable corporate kanban that organizes, automates, and tracks the entire company formation process. Each firm configures the workflow the way it works — without adapting its process to the tool.

---

## 2. TARGET AUDIENCE

The target audience is professionals who handle corporate processes in Brazil: independent accountants serving few clients who need organization without bureaucracy; small firms with teams of 2 to 5 people where processes start to slip through the cracks; and medium or large firms with teams specialized in corporate work who need centralized visibility and control.

---

## 3. CORE FEATURES

Societiza delivers the following main features:

- **Customizable kanban:** columns, stages, and statuses configurable per firm, with ready-made templates to get started.
- **Intake form:** shareable link for the client to generate a card directly in the kanban.
- **Task checklist:** tasks per stage, with automations on municipal portals (viability, operating license, etc.).
- **Event-driven notifications:** automatic emails to the accountant and the client at each relevant change in the process.
- **Smart alerts:** automatic warning when a process has been stalled beyond the expected deadline.
- **Client transparency:** real-time tracking of process status. WhatsApp integration planned for future versions.

> **Current coverage:** municipal portal automations cover cities in Santa Catarina. Expansion to other states is on the roadmap.

---

## 4. COMMUNICATION TONE

Direct, professional, and accessible. The audience has no time to waste. Communication must be practical, results-oriented, and free of unnecessary technical jargon.

Key differentiators to reinforce: the kanban that adapts to the firm's workflow, the alerts that prevent forgotten processes, and the transparency that reduces the volume of clients asking for status updates.

---

## 5. DOMAIN GLOSSARY

- **Process:** an ongoing company formation, amendment, or dissolution.
- **Card:** representation of a process in the Kanban.
- **Stage / Column:** phase of the workflow (e.g. Viability, Registration, Operating License).
- **Firm:** accounting company that is a Societiza customer (root tenant of the system).
- **Operator / User:** accountant who operates the system.
- **End client:** business owner who is forming or amending a company.
- **Viability:** preliminary consultation at the city hall to verify whether the CNAE is permitted at the address.
- **Operating License (Alvará):** operating permit issued by the city hall.
- **Template:** set of pre-configured stages and tasks for a type of process.

---

## 6. BUSINESS RULES

- A process always belongs to a firm — it never exists unattached in the system.
- A firm can have multiple users.
- Notifications are always triggered by events — never by polling.
- End client data is never exposed to other firms.
- Email is never sent directly from business handlers — always via Hangfire.
- A `WorkflowTemplate` in `Draft` status cannot be used to create processes.
- A `User` can only be created from an accepted invite (`UserInvite`) — no direct creation.

---

*Updated on 31/03/2026.*