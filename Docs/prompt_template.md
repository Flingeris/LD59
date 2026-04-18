# Prompt Template

## Purpose
Этот шаблон нужен для постановки задач Codex маленькими, управляемыми итерациями.

Его не обязательно каждый раз использовать целиком.
Можно использовать:
- полную версию для важных задач;
- сокращённую версию для обычных задач;
- мини-версию для мелких правок.

Главное — сохранять структуру:
context -> current state -> task -> constraints -> request format

---

# Full Template

## Context
Project: Bellgrave  
Engine: Unity  
Architecture style:
- Main orchestrates gameplay flow
- ServiceMain bootstraps services
- G stores only truly-global references
- UI sends intent only
- gameplay rules live in systems
- content lives in defs / CMS

## Current State
Describe what is already implemented.

Example:
- RunState exists
- phase switching exists
- HUD shows Faith / Gold / CemeteryState
- BellDef / UnitDef are not implemented yet

## Task
Describe one concrete task only.

Example:
Implement a basic BellSystem for ringing bells during the night phase.

## Constraints
List the most important rules for this task.

Example:
- do not put bell balance into Main
- UI must not spawn units directly
- use BellDef
- keep the solution simple
- no overengineering
- code should stay readable for manual expansion later

## Request
First give an implementation plan in 3–7 steps without code.  
Do not write the code yet.  
After I approve the plan, implement only the agreed scope.

---

# Short Template

Project: Bellgrave  
Current state:
[briefly describe current implementation]

Task:
[one concrete task]

Constraints:
- [rule 1]
- [rule 2]
- [rule 3]

Request:
First give a short implementation plan without code.

---

# Mini Template

Current state:
[...]

Task:
[...]

Constraints:
[...]

First give plan only, no code yet.

---

# Feature Prompt Example

Context:
Project Bellgrave in Unity.
Architecture: Main orchestrates, UI only sends intent, content through defs/CMS.

Current state:
RunState and phase switching already exist.
HUD shows Faith, Gold, and CemeteryState.

Task:
Implement a basic BellSystem that allows the player to ring a bell during the night phase and spend Faith to spawn a unit.

Constraints:
- do not hardcode bell balance in Main
- UI must not spawn units directly
- use BellDef
- keep it simple and easy to extend later

Request:
First give an implementation plan in 5 steps without code.
Do not implement yet.

---

# Refactor Prompt Example

Context:
Project Bellgrave in Unity.

Current state:
Bell ringing works, but Main has become too large and now contains validation, resource spending, and direct spawn calls.

Task:
Refactor the current bell flow so that Main stays an orchestrator and the bell logic moves into a dedicated BellSystem.

Constraints:
- do not rewrite unrelated systems
- keep current behavior unchanged
- preserve readability
- avoid large structural changes outside bell flow

Request:
First analyze the current problem and propose a refactor plan in steps.
Do not write code yet.

---

# Bugfix Prompt Example

Current state:
Bell button sometimes spends Faith, but unit does not spawn if the phase changes on the same frame.

Task:
Find the likely cause and propose a minimal fix.

Constraints:
- do not redesign the whole phase system
- prefer a local fix
- keep architecture intact

Request:
First explain the likely cause and propose the smallest safe fix.
No code yet.

---

# Important Prompt Rules

## 1. One task at a time
Do not ask Codex to implement several large systems at once.

## 2. Always include current state
Codex should know what already exists.

## 3. Always include constraints
Especially architectural ones.

## 4. Prefer plan first
This is the default mode.

## 5. Ask for agreed scope only
After plan approval, ask to implement only the exact agreed step.

## 6. Keep prompts concrete
Avoid vague requests like:
- make combat better
- improve architecture
- add progression

Prefer:
- add end-of-night condition
- implement UpgradeDef and purchase flow
- move faith spending from UI into ResourceSystem

---

# Recommended Working Pattern

1. Write a task using the short or full template.
2. Ask for plan only.
3. Review the plan.
4. Correct the plan if needed.
5. Ask Codex to implement the approved scope.
6. Test in Unity.
7. If needed, ask for a small follow-up fix.
8. Move to the next task.

---

# Personal Rule For This Project
If a task feels too big to clearly fit in one prompt, split it before sending it to Codex.