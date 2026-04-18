# Tasklist

## Goal
Собрать играбельный MVP Bellgrave за ограниченное джемовое время.

Главный приоритет:
- сначала рабочий core loop;
- потом базовая читаемая подача;
- потом только полировка и nice-to-have.

---

# Phase 0 — Project Setup

## Task 0.1 — Prepare base project structure
Сделать минимальный каркас проекта:
- gameplay scene
- ServiceMain bootstrap
- Main
- G bindings
- базовый HUD / UI root
- базовые папки проекта

Done when:
- сцена запускается;
- ServiceMain поднимается;
- Main доступен;
- HUD и UI могут быть привязаны;
- проект готов к следующему шагу.

## Task 0.2 — Create runtime state model
Сделать `RunState` и связанные runtime-модели.

Минимум:
- Faith
- Gold
- CemeteryState
- CurrentDay
- CurrentNight
- CurrentPhase

Done when:
- все ключевые значения текущего забега лежат в одном понятном state-объекте;
- Main может читать и менять этот state.

---

# Phase 1 — Skeleton Of The Core Loop

## Task 1.1 — Add phase system
Сделать базовое переключение фаз:
- Day
- Night
- Result / Transition step if needed

Done when:
- Main умеет явно запускать день;
- Main умеет явно запускать ночь;
- текущая фаза хранится явно;
- в debug-режиме можно руками пройти цикл день -> ночь -> день.

## Task 1.2 — Add temporary debug controls
Сделать временные debug-кнопки / клавиши:
- start night
- end night
- add faith
- add gold
- damage cemetery

Done when:
- основные состояния можно быстро тестировать без полной игры.

Важно:
эти штуки потом можно удалить или спрятать под debug flag.

## Task 1.3 — Add HUD resource display
Сделать отображение:
- Faith
- Gold
- CemeteryState
- Current phase

Done when:
- HUD всегда отражает актуальный runtime-state.

---

# Phase 2 — Night Gameplay Base

## Task 2.1 — Add bell definitions
Сделать `BellDef`.

Минимум в def:
- id
- display name
- faith cost
- linked unit id
- icon / optional presentation refs

MVP bells:
- cheap bell
- medium bell
- heavy bell

Done when:
- колокола существуют как контент, а не хардкод в Main.

## Task 2.2 — Add unit and enemy definitions
Сделать:
- `UnitDef`
- `EnemyDef`

Минимум:
- hp
- damage
- attack interval
- move speed
- prefab/view ref if needed

Done when:
- основные боевые сущности описаны через defs.

## Task 2.3 — Implement BellSystem
Сделать базовый `BellSystem`.

Он должен:
- принимать bell id;
- проверять phase;
- проверять faith;
- списывать faith;
- создавать юнита.

Done when:
- можно нажать bell button и вызвать нужного юнита;
- при нехватке faith вызова не происходит.

## Task 2.4 — Create lane prototype
Сделать одну боевую линию.

Минимум:
- точка спавна юнитов игрока;
- точка спавна врагов;
- движение навстречу;
- встреча;
- бой по таймингу;
- смерть;
- продолжение движения победителя.

Done when:
- один юнит и один враг могут полностью разыграть бой от спавна до смерти.

## Task 2.5 — Implement enemy spawning
Сделать базовый `WaveSystem`.

MVP:
- фиксированная волна;
- враги спавнятся по времени;
- по одному типу/нескольким простым типам.

Done when:
- ночью враги автоматически появляются без ручного вмешательства.

## Task 2.6 — Connect bells to night defense loop
Соединить:
- bell buttons
- BellSystem
- lane
- enemy spawn
- cemetery damage on breakthrough

Done when:
- ночью игрок реально тратит faith на вызов защитников;
- если не защищаться, враги доходят и бьют кладбище.

## Task 2.7 — Add end-of-night condition
Сделать условие завершения ночи.

Например:
- все враги волны заспавнены;
- все враги убиты или ушли;
- линия больше не активна.

Done when:
- ночь может завершиться автоматически;
- после этого начинается переход к дневной фазе.

---

# Phase 3 — Day Gameplay Base

## Task 3.1 — Implement basic day reward flow
После ночи начислять:
- gold reward
- faith reward

На первом этапе можно упростить:
- фиксированная faith за день
- gold based on survived night / kills

Done when:
- после ночи игрок получает ресурсы.

## Task 3.2 — Create simple day screen
Сделать простой day/meta screen.

Показать:
- итоги ночи
- текущее количество faith
- текущее количество gold
- кнопка перехода к следующей ночи

Done when:
- между ночами есть понятная управляемая пауза.

## Task 3.3 — Add upgrade definitions
Сделать `UpgradeDef`.

Минимум:
- id
- name
- price
- effect type
- effect value

Done when:
- апгрейды существуют как данные.

## Task 3.4 — Implement UpgradeSystem
Сделать базовую покупку апгрейдов.

MVP effects:
- +faith income
- +cemetery max state or repair
- unlock bell
- cheaper bell / stronger unit

Done when:
- игрок может купить апгрейд за gold;
- эффект реально применяется к run.

## Task 3.5 — Connect day phase to progression
Соединить:
- day screen
- current resources
- available upgrades
- next night button

Done when:
- дневная фаза уже ощущается как часть петли, а не просто пауза.

---

# Phase 4 — Full Core Loop Validation

## Task 4.1 — Complete full playable loop
Проверить полный цикл:

day ->
buy or skip ->
start night ->
defend with bells ->
night end ->
gain rewards ->
next day

Done when:
- игру можно сыграть минимум 3–5 ночей подряд без ручных костылей.

## Task 4.2 — Add lose condition
Сделать поражение при падении CemeteryState до нуля.

Done when:
- игра завершается корректно;
- показывается defeat screen;
- есть restart.

## Task 4.3 — Add simple win / survival target
Для джема лучше иметь понятную цель.

Варианты:
- survive 5 nights
- survive 7 nights
- endless with score

Предпочтительно для MVP:
- survive fixed number of nights

Done when:
- у игры есть чёткая рамка прохождения.

---

# Phase 5 — Juice And Readability

## Task 5.1 — Add bell feedback
Добавить:
- звук колокола
- простую анимацию кнопки / объекта
- popup if not enough faith

Done when:
- звон ощущается как событие.

## Task 5.2 — Add combat feedback
Добавить:
- hit flashes
- damage popups or simple feedback
- death feedback
- simple VFX if cheap

Done when:
- бой читается визуально.

## Task 5.3 — Add phase transition feedback
Добавить:
- переход день/ночь
- тёмный оверлей / fade
- текстовые баннеры
- атмосфера смены времени суток

Done when:
- цикл ощущается целостным.

## Task 5.4 — Improve HUD readability
Проверить:
- понятно ли где faith
- понятно ли где gold
- видно ли состояние кладбища
- видно ли какой bell дорогой / дешёвый

Done when:
- новичок понимает интерфейс без объяснения.

---

# Phase 6 — Balance And Cleanup

## Task 6.1 — Balance first 10 minutes
Настроить:
- faith income
- bell costs
- enemy pressure
- upgrade prices
- cemetery durability

Done when:
- первые несколько ночей не слишком лёгкие и не слишком душные.

## Task 6.2 — Remove obvious code duplication
Сделать точечный рефакторинг:
- повторы
- лишние прямые зависимости
- слишком жирные методы
- неясные имена

Done when:
- код проще читать;
- ничего не сломано.

## Task 6.3 — Clean Main if needed
Если `Main` распух:
- вынести локальную логику в отдельные системы;
- оставить в Main orchestration only.

Done when:
- Main читабелен сверху вниз как сценарий игры.

## Task 6.4 — Final bug pass
Проверить:
- restart
- phase transitions
- resource spending
- wave end
- upgrade application
- lose/win

Done when:
- игра не ломается на базовом прохождении.

---

# Nice To Have After MVP

## Optional 1 — Faith depends on cemetery condition
Чем хуже защищено кладбище, тем меньше faith днём.

## Optional 2 — Second lane
Только если первая линия уже отлично работает.

## Optional 3 — More enemy variety
Только после базового баланса.

## Optional 4 — Special bell cooldowns
Только если нужна дополнительная глубина.

## Optional 5 — Better day atmosphere
Текстовые события, посетители, flavour.

---

# Priority Order

## Absolute Priority
1. RunState
2. Day/Night phases
3. HUD resources
4. BellSystem
5. One lane combat
6. Enemy wave
7. Cemetery damage
8. Night end
9. Day rewards
10. Upgrades
11. Lose/win

## Secondary Priority
12. feedback
13. balancing
14. cleanup

## Low Priority
15. extra content
16. extra lanes
17. richer day simulation

---

# Suggested First Implementation Sequence For Codex
1. RunState + phase enum
2. Main day/night switching
3. HUD resource text
4. BellDef / UnitDef / EnemyDef
5. BellSystem with faith spending
6. one lane prototype
7. enemy spawning
8. cemetery damage
9. end-of-night flow
10. day reward screen
11. UpgradeSystem
12. lose/win
13. polish