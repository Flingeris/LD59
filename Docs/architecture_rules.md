# Architecture Rules

## Goal
Архитектура проекта должна быть:
- простой для джема;
- понятной автору без долгого вхождения;
- расширяемой после джема;
- совместимой с текущим каркасом Main / ServiceMain / G / CMS.

Проект не строится как “идеально чистая архитектура”.
Проект строится как быстрый, читаемый и управляемый gameplay-каркас.

---

## Core Principle
Main управляет игровым циклом, но не должен превращаться в свалку всей логики проекта.

Идея:
- Main orchestrates
- systems calculate
- UI displays
- defs describe content

---

## Main Roles

### Main
Main — центральный оркестратор run-time логики.

Main отвечает за:
- запуск run;
- переключение day / night;
- переходы между фазами;
- общую проверку win / lose;
- вызов систем;
- приём player intent из UI;
- координацию порядка обновления игры.

Main не должен:
- хранить всю боёвку внутри себя;
- вручную считать каждый боевой обмен;
- напрямую менять UI-детали по всему проекту;
- содержать defs и контентные данные;
- быть местом, где живут все правила игры.

Правило:
если логика может быть оформлена как отдельная система, она должна быть вынесена из Main.

---

### ServiceMain
ServiceMain — bootstrap и точка поднятия глобальных сервисов.

ServiceMain отвечает за:
- создание и инициализацию глобальных систем;
- регистрацию ссылок в G;
- запуск CMS;
- запуск AudioSystem;
- запуск других truly-global сервисов.

ServiceMain не должен:
- управлять игровым циклом;
- хранить state run;
- содержать gameplay-логику боя, волн, экономики.

---

### G
G — лёгкий глобальный реестр ссылок на ключевые сервисы и root-объекты.

G допустим в этом проекте, потому что:
- проект джемовый;
- автор уже работает в таком стиле;
- это ускоряет навигацию и сборку фич.

В G можно хранить:
- Main
- ServiceMain
- HUD
- UI
- AudioSystem
- ScreenFader
- GameFeel
- другие few truly-global references

В G нельзя превращать:
- временные боевые сущности;
- локальные зависимости конкретной фазы;
- случайные ссылки “на всякий случай”.

Правило:
если ссылка не глобальна по смыслу — не класть её в G.

---

## Content Rules

### CMS and Defs
Контент проекта должен описываться через defs, а не жёстко в Main.

Через CMS должны подгружаться:
- BellDef
- UnitDef
- EnemyDef
- UpgradeDef
- WaveDef
- возможно DayEventDef

Правила:
- у каждого def должен быть уникальный id;
- код обращается к def по id или typed-access;
- gameplay не должен хардкодить баланс в Main;
- контентные различия между юнитами и колоколами живут в defs.

---

## Layer Separation

### 1. Data Layer
Содержит definitions и runtime-state модели.

Примеры:
- BellDef
- UnitDef
- EnemyDef
- UpgradeDef
- WaveDef
- RunState
- NightState
- DayRewardData

Data layer не знает про UI.

---

### 2. Logic Layer
Содержит правила игры и вычисления.

Примеры:
- BellSystem
- WaveSystem
- ResourceSystem
- CombatSystem
- UpgradeSystem
- CemeteryStateSystem
- DayPhaseSystem
- NightPhaseSystem

Logic layer:
- принимает state и команды;
- меняет state;
- может вызывать сервисы или события;
- не должен напрямую верстать UI.

---

### 3. Presentation Layer
Содержит отображение и пользовательский ввод.

Примеры:
- HUD
- UI
- BellButtonView
- ResourcePanel
- CemeteryStateBar
- Popup views
- Lane visuals
- Unit/enemy views

Presentation layer:
- показывает состояние;
- отправляет intent;
- не решает правила игры самостоятельно.

---

## UI Rule
UI никогда не является источником gameplay-истины.

UI может:
- отправить “TryRingBell”
- отправить “TryBuyUpgrade”
- отправить “StartNight”
- отправить “ContinueAfterDay”

UI не может:
- сама списывать ресурсы;
- сама спавнить юнита;
- сама завершать ночь;
- сама решать, доступен ли апгрейд.

Правильный поток:
UI -> Main / System intent -> validation -> state change -> UI refresh

---

## Run State
Все ключевые параметры текущего забега должны храниться в явной runtime-модели.

Минимум:
- Faith
- Gold
- CemeteryState
- CurrentDayIndex
- CurrentNightIndex
- unlocked bells
- purchased upgrades
- текущий phase state

Правило:
не размазывать состояние run по десяти MonoBehaviour, если это core state игры.

---

## Phase Architecture
Фазы игры — это важнейшая часть проекта, поэтому они должны быть явными.

Нужны как минимум:
- Day phase
- Night phase
- Result / transition steps between them

Main должен явно знать:
- какая фаза активна;
- какие действия сейчас разрешены;
- когда фаза начинается;
- когда фаза завершается.

Правило:
никаких “магических” переходов, размазанных по кнопкам и вьюхам.

---

## Recommended Systems

### ResourceSystem
Отвечает за:
- faith;
- gold;
- spending checks;
- adding rewards.

### CemeteryStateSystem
Отвечает за:
- общее состояние кладбища;
- урон кладбищу при прорывах;
- defeat condition.

### BellSystem
Отвечает за:
- вызов юнитов через bell id;
- проверку стоимости;
- cooldown / ограничения, если появятся;
- выбор соответствующего UnitDef.

### WaveSystem
Отвечает за:
- запуск ночной волны;
- спавн врагов;
- отслеживание завершения волны.

### Combat / Lane System
Отвечает за:
- движение сущностей по линии;
- встречи;
- удар по таймингу;
- смерть;
- проход врага к кладбищу.

### UpgradeSystem
Отвечает за:
- список доступных улучшений;
- покупку;
- применение эффекта улучшения.

---

## MonoBehaviour Rules
MonoBehaviour используется как:
- entry point;
- scene binding;
- visual host;
- lifecycle container;
- lightweight controller.

MonoBehaviour не должен без нужды становиться:
- хранилищем большого количества правил;
- единственным местом бизнес-логики;
- god object вне Main.

Если класс в основном считает правила и почти не зависит от Unity scene API, лучше сделать его plain C# class.

---

## ScriptableObject Rules
ScriptableObject используется для контента и настроек, а не для мутирующего runtime-state.

Можно хранить:
- defs;
- баланс;
- настройки сигналов;
- описания юнитов;
- визуальные и аудио ссылки.

Нельзя хранить как главный источник:
- текущую faith;
- текущее состояние ночи;
- временные боевые значения run.

---

## Naming Rules
Использовать понятные и прямые названия.

Предпочтительно:
- BellDef
- UnitDef
- EnemyDef
- WaveDef
- RunState
- BellSystem
- NightPhaseController
- CemeteryStateSystem

Избегать:
- overly-generic names вроде DataManager, GameController2, TempSystem, HelperManager.

---

## Scene Rules
Для MVP желательно минимальное количество сцен.

Предпочтительно:
- одна основная gameplay scene;
- сервисы поднимаются отдельно через ServiceMain;
- UI и gameplay находятся в управляемом, предсказуемом составе.

Не плодить сцены без реальной необходимости.

---

## Audio / Feel Rules
AudioSystem и GameFeel остаются отдельными сервисами, не смешиваются с gameplay-правилами. Такой паттерн у тебя уже есть и он удачно отделяет сервисный слой от основного цикла. :contentReference[oaicite:2]{index=2} :contentReference[oaicite:3]{index=3}

Gameplay-логика может говорить:
- play bell sound;
- play hit sound;
- spawn popup;
- shake object;

Но не должна знать детали устройства аудио-пула или tween-реализации. У тебя аудио уже отделено на players/pool/container, а контентный слой CMS отдельно валидирует и загружает defs, что хорошо ложится на такой подход. :contentReference[oaicite:4]{index=4} :contentReference[oaicite:5]{index=5}

---

## What We Keep From Existing Architecture
Из текущего подхода сохраняем:
- Main как оркестратор;
- ServiceMain как bootstrap;
- G как лёгкий global access;
- CMS как единый способ доступа к defs;
- отдельный AudioSystem;
- отдельный HUD / UI слой.

Это не теория, а реально повторяющийся паттерн в обоих твоих файлах. В каркасе уже есть `G.main`, `G.ServiceMain`, `G.audioSystem`, `G.HUD`, `G.ui`, а `ServiceMain` поднимает сервисы и инициализирует CMS. В большом проекте тот же каркас повторяется и расширяется. :contentReference[oaicite:6]{index=6} :contentReference[oaicite:7]{index=7}

---

## What We Do NOT Carry Over
В новый Bellgrave нельзя переносить вслепую:
- слишком жирный Main;
- избыточную многослойность;
- UI, который сам рулит игровым циклом;
- хардкод контента в коде;
- преждевременную сложность, не нужную MVP.

Главный приоритет: speed + readability + expandability.

---

## Architectural Red Flags
Плохой признак, если:
- Main начинает знать всё обо всём;
- UI списывает ресурсы сам;
- Bell buttons напрямую спавнят юнитов;
- Faith / Gold хранятся в разных случайных компонентах;
- enemy / unit balance захардкожен в Main;
- G превращается в свалку ссылок;
- каждая фича требует лезть в 10 мест.

Если так произошло — сначала упрощаем, потом расширяем.

---

## Bellgrave-Specific Flow
Правильный поток действия ночью:

1. Игрок нажимает bell button в HUD.
2. HUD отправляет intent в Main.
3. Main делегирует вызов BellSystem.
4. BellSystem проверяет правила:
   - активна ли night phase;
   - хватает ли faith;
   - доступен ли bell;
5. При успехе:
   - списывается faith;
   - спавнится юнит;
   - играет звук;
   - обновляется HUD.
6. При неуспехе:
   - показывается popup / feedback;
   - state не меняется.

Это базовый паттерн для всех player actions.

---

## Preferred Implementation Style for This Project
Нужен pragmatic architecture:
- не переусложнять;
- не писать “на соплях”;
- держать Main тонким;
- держать state явным;
- держать UI тупым;
- держать контент в defs;
- дробить задачи на маленькие проверяемые шаги.

Если есть выбор между “чуть менее красиво, но понятно и быстро” и “очень красиво, но тяжело поддерживать в джемовом темпе” — выбирать первое.