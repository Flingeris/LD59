# Prompt Template

## Purpose
Этот шаблон нужен для постановки задач Codex маленькими, управляемыми итерациями для Bellgrave.

Главное правило:
- сначала план;
- потом реализация;
- одна маленькая задача за раз;
- не ломать текущий каркас проекта;
- не откатываться к старой модели, где gameplay идёт напрямую из HUD.

Структура:
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

Important current design direction:
- night gameplay is now management-defense
- keeper moves between points of interest
- actions are restricted by keeper position
- bells are world interactions, not pure HUD actions
- day phase stays short and meta-oriented

## Current State
Describe only what already exists right now.

Example:
- lane combat exists
- enemy waves exist
- cemetery hp exists
- bell ringing currently still works from old flow
- keeper movement does not exist yet
- faith is still mostly fixed for the whole night

## Task
Describe one concrete task only.

Example:
Implement keeper movement between 3 night points of interest.

## Constraints
List the key rules for this task.

Example:
- do not redesign unrelated combat systems
- keep Main as orchestrator
- do not move gameplay authority into UI
- do not overengineer pathfinding; simple point-to-point movement is enough
- keep the solution readable for manual iteration
- if this task is large, split it into smaller implementation steps first

## Request
First give an implementation plan in 3–7 steps without code.  
Do not write code yet.  
After I approve the plan, implement only the agreed scope.

---

# Short Template

Project: Bellgrave

Current state:
[briefly describe current implementation]

Task:
[one concrete task]

Constraints:
- Main remains orchestrator
- UI only sends intent
- no rollback to old HUD-driven bell architecture
- keep scope minimal
- no overengineering

Request:
First give a short implementation plan without code.

---

# Mini Template

Current state:
[...]

Task:
[...]

Constraints:
- keep current architecture
- do not expand scope
- plan first, no code

Request:
First give a short plan only.

---

# Feature Prompt Example

Context:
Project Bellgrave in Unity.
Architecture: Main orchestrates, UI only sends intent, content through defs/CMS.
Night gameplay is now based on keeper movement between points of interest.

Current state:
Basic lane combat and enemy spawning already exist.
Cemetery damage exists.
Bell ringing still follows the old direct flow.
There is no keeper movement yet.

Task:
Implement a basic keeper movement flow between Bells Point, Faith Point, and Repair Point.

Constraints:
- do not redesign lane combat
- do not move logic into UI
- keep movement simple and explicit
- Main should coordinate, but movement logic can live outside Main
- keep this task focused only on movement and arrival state

Request:
First give an implementation plan in 5 steps without code.
Do not implement yet.

---

# Refactor Prompt Example

Context:
Project Bellgrave in Unity.

Current state:
Bell ringing works, but it is still tied too directly to the old interaction flow and is not restricted by keeper position.

Task:
Refactor bell usage so that ringing is only possible when the keeper is inside the bells area.

Constraints:
- keep current bell content intact
- do not redesign the whole resource system
- preserve readability
- avoid unrelated changes
- keep Main as orchestrator

Request:
First analyze the current flow and propose a minimal refactor plan.
Do not write code yet.

---

# Bugfix Prompt Example

Current state:
The keeper is supposed to unlock interaction after reaching a point of interest, but sometimes the game still thinks he is moving and the action stays blocked.

Task:
Find the likely cause and propose a minimal safe fix.

Constraints:
- do not redesign the entire movement system
- prefer a local fix
- preserve the current architecture

Request:
First explain the likely cause and propose the smallest safe fix.
No code yet.

---

# Important Prompt Rules

## 1. One task at a time
Do not ask Codex to implement several large systems at once.

## 2. Always include current state
Codex should know what already exists and what still belongs to the old version.

## 3. Always include constraints
Especially architectural ones and scope limits.

## 4. Prefer plan first
This is the default mode.

## 5. Ask for agreed scope only
After plan approval, ask to implement only the exact agreed step.

## 6. Keep prompts concrete
Avoid vague requests like:
- make night gameplay deeper
- improve management
- add progression

Prefer:
- add keeper movement
- gate bell usage by keeper position
- add faith collection point
- rework breakthrough enemies to persist at cemetery
- move gold rewards to enemy kills

---

# Recommended Working Pattern

1. Write one concrete task.
2. Ask for plan only.
3. Review the plan.
4. Correct the plan if needed.
5. Ask Codex to implement only the approved scope.
6. Test in Unity.
7. Request a small follow-up fix if needed.
8. Move to the next task.

---

# Personal Rule For This Project
If a task feels too big to clearly fit in one prompt, split it before sending it to Codex.

For Bellgrave specifically:
prefer implementation order that strengthens the new night loop first:
keeper -> POI -> gated actions -> faith collection -> breakthrough pressure -> progression