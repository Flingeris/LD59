# Prompt Template

## Purpose
Этот файл нужен для быстрых и удобных рабочих запросов к Codex по Bellgrave.

Главная цель:
- ставить задачи коротко;
- двигаться маленькими шагами;
- не терять архитектурный каркас;
- не заставлять пользователя каждый раз писать огромный формальный prompt.

Основной режим работы:
- первый запрос по новой задаче может быть чуть подробнее;
- все следующие запросы должны быть короткими и естественными;
- по умолчанию Codex должен понимать, что работа идёт итеративно.

---

## Core Rule
Для Bellgrave промпт не должен быть длинным ради длины.

Хороший промпт:
- короткий;
- конкретный;
- про один шаг;
- с понятными ограничениями;
- без повторения всего проекта каждый раз.

Если контекст уже есть в проектных файлах и текущем чате, не нужно каждый раз переписывать его заново.

---

## Main Prompt Types

### 1. Start Prompt
Используй, когда начинается новая задача или новый кусок рефакторинга.

Формат:

Current state:
[что уже есть сейчас]

Task:
[одна конкретная задача]

Constraints:
- [только важные ограничения]

Request:
First give a short implementation plan without code.

---

### 2. Next Step Prompt
Это основной рабочий формат после согласования плана.

Формат:

Do the next step from the agreed plan:
[название или краткое описание шага]

Constraints:
- [если нужно]
- keep scope small
- do not touch unrelated systems

---

### 3. Fix Prompt
Используй для локальной проблемы.

Формат:

Current problem:
[что именно не так]

Task:
Find the likely cause and propose or implement the smallest safe fix.

Constraints:
- do not redesign the whole system
- prefer local fix
- preserve current architecture

---

### 4. Refactor Prompt
Используй, если задача именно в упрощении или чистке.

Формат:

Current state:
[что есть]
[почему это неудобно]

Task:
Refactor this into a simpler version.

Constraints:
- preserve behavior
- keep architecture
- keep scope minimal
- no giant rewrite

Request:
First analyze the current flow and propose a minimal refactor plan.

---

### 5. Review Prompt
Используй, когда нужно проверить текущий этап по tasklist или требованиям.

Формат:

Check current implementation against:
- [phase / tasklist / requirement]

Request:
Briefly list:
- what is already done
- what is missing
- what is partially done
- what should be the next step

Do not write code yet.

---

## Minimal Working Prompts
Большинство follow-up сообщений должны быть короткими.

Допустимые рабочие варианты:
- Do the next step from the agreed plan: [шаг]
- Implement only this part: [кусок работы]
- Fix this with the smallest safe change
- Check current state and tell me the next logical step
- Refactor this into a simpler version
- Do not touch unrelated systems
- Keep scope small

---

## What NOT To Do
Не нужно каждый раз:
- заново пересказывать весь проект;
- заново описывать Main / ServiceMain / G / CMS;
- писать огромный formal prompt;
- перечислять все прошлые решения, если они уже есть в чате;
- просить несколько больших систем сразу.

Плохо:
- сделать всю ночную фазу
- переделать весь UI
- отрефакторить всю боёвку и прогрессию
- сделать красиво и универсально

Хорошо:
- сделать следующий шаг
- только этот кусок
- локально упростить
- проверить текущий этап
- исправить без редизайна всего

---

## Bellgrave-Specific Prompting Rule
Для Bellgrave по умолчанию лучше писать короткие живые промпты, а не длинные формальные.

Если задача уже обсуждалась в текущем чате, короткий follow-up промпт — это нормальный режим работы.

---

## Default Expectations From Codex
Даже если пользователь пишет очень коротко, Codex должен по умолчанию понимать следующее:

- задача делается маленьким шагом;
- без лишнего расширения scope;
- без giant-refactor без просьбы;
- с уважением к текущей архитектуре;
- сначала план, если задача не совсем тривиальна;
- после реализации — короткий отчёт, что изменено и что дальше.

---

## Final Rule
Prompt template для Bellgrave должен помогать работать быстрее, а не заставлять писать длинные промпты ради формы.

Если короткий промпт уже однозначно выражает задачу, этого достаточно.