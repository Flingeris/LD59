# Tasklist

## Goal
Перевести Bellgrave из простого bell-defense prototype в playable management-defense MVP, где основная глубина рождается ночью через управление хранителем, перемещение между точками интереса и конфликт между призывом, экономикой и ремонтом.

Главный приоритет:
- сначала новый night core loop;
- потом подключение прогрессии;
- потом полировка и nice-to-have.

---

# Phase A — Reframe The Night Loop

## Task A1 — Add Keeper runtime model
Сделать runtime-модель хранителя.

Минимум:
- current position
- move speed
- current target point
- current interaction state
- availability flags if needed

Done when:
- в runtime есть явное состояние хранителя;
- Main может понимать, где хранитель находится и чем занят.

## Task A2 — Add keeper scene actor and movement
Сделать хранителя как игрового агента на карте.

Минимум:
- scene object / prefab
- движение между точками
- простое прибытие в цель
- понятный API для команды move to point

Done when:
- хранитель реально перемещается по игровому полю;
- действия больше не происходят мгновенно из любой точки.

## Task A3 — Add points of interest
Сделать 3 базовые точки интереса ночи:
- Bells
- Faith Point
- Cemetery Repair Point

Done when:
- каждая точка существует в сцене явно;
- хранителя можно направить к ней;
- они готовы к подключению логики.

## Task A4 — Restrict night actions by keeper position
Подключить главное правило:
- bell actions доступны только у bell point;
- faith interaction доступен только у faith point;
- repair доступен только у repair point.

Done when:
- позиция хранителя реально ограничивает доступные действия;
- spatial-management loop начал существовать.

---

# Phase B — Make Night Resource And Pressure Real

## Task B1 — Rework faith flow for night collection
Переделать faith так, чтобы она была не почти фиксированной на ночь, а добывалась / собиралась во время ночи через faith point.

MVP:
- можно оставить маленький стартовый запас faith;
- основной приток должен быть привязан к точке веры.

Done when:
- ночью игроку нужно идти за верой;
- экономика стала частью ночного решения.

## Task B2 — Add bell world interaction
Перенести bell interaction в world-space flow.

Минимум:
- колокол как scene object
- interaction через collider / click routing
- больше не использовать bell как чисто HUD-driven gameplay action

Done when:
- сигнал подаётся через объект в мире;
- bell flow соответствует новой fantasy.

## Task B3 — Add bell cooldowns
Добавить cooldown на bell usage.

Done when:
- bell нельзя спамить без паузы;
- между bell actions появляется окно для других решений.

## Task B4 — Add night timer and wave forecast
Сделать отображение:
- общей длительности ночи;
- примерного времени до следующей волны / подволны.

Done when:
- игрок может планировать, когда идти за верой, когда чинить, когда возвращаться к bell zone.

---

# Phase C — Make Breakthrough A Persistent Problem

## Task C1 — Rework breakthrough behavior
Переделать поведение врага при достижении кладбища.

Вместо мгновенного исчезновения:
- враг остаётся у кладбища;
- входит в состояние attack cemetery;
- продолжает наносить урон со временем.

Done when:
- прорыв врага создаёт постоянную проблему;
- ремонт и возврат к bell начинают конкурировать по приоритету.

## Task C2 — Connect cemetery damage pressure loop
Связать:
- прорыв врага;
- persistent damage;
- repair point;
- defeat condition.

Done when:
- у игрока есть реальный кризисный сценарий;
- игнорировать кладбище становится опасно.

---

# Phase D — Tie Economy To Combat Outcome

## Task D1 — Move gold gain to enemy kills
Переделать основную выдачу золота:
- золото за убийство врагов;
- не опираться только на завершение ночи.

Done when:
- хорошая оборона и эффективный призыв напрямую кормят прогрессию.

## Task D2 — Validate full night loop
Проверить полный ночной цикл:

move between points ->
collect faith ->
ring bells near bells ->
hold waves ->
repair cemetery if needed ->
earn gold from kills ->
finish night

Done when:
- ночь уже ощущается как management-defense gameplay;
- минимум одна ночь играется без ручных костылей.

---

# Phase E — Adapt Day Progression To New Loop

## Task E1 — Reframe upgrade effects for new design
Пересобрать upgrade pool под новый цикл.

Приоритетные эффекты:
- increase faith collection
- increase starting faith
- improve skeleton bell output
- increase cemetery max hp
- instant repair
- increase skeleton lifetime
- extend skeleton lifetime on kill
- increase keeper move speed

Done when:
- апгрейды усиливают именно новый night loop, а не старую модель.

## Task E2 — Connect day screen to new progression
Обновить day phase так, чтобы она усиливала:
- keeper
- faith economy
- cemetery sustain
- bell efficiency

Done when:
- день работает как короткая meta-фаза подготовки к следующей ночи.

---

# Phase F — Secondary Technical / Juice Tasks

## Task F1 — Add visual coin pickup effect
Сделать визуальный эффект:
- монеты выпадают в мир;
- затем летят в HUD;
- число на ресурсе может прибавляться не строго по количеству визуальных монет.

Done when:
- золото визуально ощущается как награда за бой.

## Task F2 — Improve world interaction robustness
Закрепить world input flow:
- collider-based interaction
- Physics2DRaycaster on camera if required by final input stack
- no gameplay dependence on canvas resolution behavior

Done when:
- world actions стабильно работают на разных разрешениях.

---

# Must-Have Priority Order

## Absolute Priority
1. Keeper runtime state
2. Keeper movement between night points
3. Three points of interest
4. Action restriction by keeper position
5. Night faith collection loop
6. Bell cooldown
7. Night timer and next-wave readability
8. Persistent breakthrough enemies
9. Repair loop connection
10. Gold from kills
11. Upgrade pool adaptation

## Secondary Priority
12. visual coin pickup
13. world interaction robustness cleanup
14. balance and cleanup

## Lower Priority
15. assistant collector
16. random upgrade choices
17. new bells
18. new unit types
19. grave-based population cap

---

# Suggested First Implementation Sequence For Codex
1. Add Keeper runtime model
2. Add keeper movement actor in scene
3. Add three POI anchors
4. Restrict night actions by keeper position
5. Rework faith into night collection
6. Add bell cooldown
7. Add night timer + next wave forecast
8. Rework enemy breakthrough into persistent cemetery attack
9. Move gold gain to enemy kills
10. Update upgrades for new loop
11. Validate full playable night loop
12. Add secondary polish tasks