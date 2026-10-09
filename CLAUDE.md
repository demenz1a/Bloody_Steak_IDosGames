# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Проект

**Bloody Steak** — 2D-игра на Unity: симулятор кухни со стелс-убийствами (смесь Overcooked и Party Hard). Игрок на кухне — «Овца» (готовит), в зале — «Волк» (может убивать посетителей, чьи трупы перерабатываются в мясо).

- Unity **6000.3.6f1** (Unity 6), URP 2D, Cinemachine 3, TextMesh Pro, NavMeshPlus (`Assets/NavMeshComponents`, свой asmdef).
- Весь игровой код — в `Assembly-CSharp`, без asmdef. Тестов нет.
- Сборка, запуск и проверка компиляции — только через Unity Editor (или `/unity:unity-cli`). Отдельных команд build/lint/test в репозитории нет.
- Git: в PATH нет, работает через GitHub Desktop (`%LOCALAPPDATA%\GitHubDesktop\app-*\resources\app\git\cmd\git.exe`). Remote: `github.com/demenz1a/Bloody_Steak_IDosGames`.

## Старый и новый код

Проект в процессе переписывания:
- **Актуальный код:** `Assets/Scripts/` (корень, `Player/`, `NPCAndOrder/`). Актуальная сцена: `Assets/Scenes/GameScene.unity`.
- **Устаревший код:** `Assets/Scripts/OldScripts/` (все классы с префиксом `Old*`) и сцены в `Assets/Scenes/OldScenes/`. Не расширяй и не используй их в новом коде. Build Settings пока ссылаются **только на старые сцены**.

## Архитектура (актуальный код)

**Взаимодействие.** Всё, с чем игрок взаимодействует по Space, реализует `IInteractable` (`CanInteract` / `Interact`). `PlayerInteractor` каждый кадр делает `OverlapCircleAll` перед игроком по `FacingDirection`, отбирает объекты с `CanInteract == true` и выбирает ближайший. Подсветка (`InteractionHighlightView` + шейдер `Assets/SpriteOutline.shader` через MaterialPropertyBlock) поэтому всегда соответствует реальной доступности действия. Вся логика доступности (состояние объекта, что в руках, форма игрока) живёт в `CanInteract`. `Interact` вызывается только после `true` и не перепроверяет условия.

**Предметы и перенос.** `CarriableItem` → `Product`. Предметы не имеют физики, они перепривязываются между `HoldPoint` (руки игрока в `PlayerInventory`, слоты `MeatTable`, точка `CookingStation`). Один и тот же экземпляр `Product` живёт от сырого мяса до блюда: станция вызывает `SetType(ProductType)`, а продукт сам меняет спрайт. Мясо создаётся только в `MeatTable` и уничтожается только в `TrashBin` или при выдаче заказа.

**Готовка.** Гриль, шампуры и мясорубка — один компонент `CookingStation` с разным массивом `CookingStage[]` в инспекторе. Новую станцию обычно настраивают данными, а не новым скриптом. Фазы: Empty → Cooking → ReadyToServe → Burnt.

**Формы игрока.** `PlayerFormController` (Sheep/Wolf) переключают триггеры `FormZone`: на кухне Sheep, в зале Wolf, в буферной кладовке зоны нет. Подписчики события `OnFormChanged`: `CameraFormSwitcher` (приоритеты Cinemachine-камер) и `OrderManager`. Форма влияет на правила: в форме Wolf таймеры заказов идут медленнее (`wolfTimeScale`), новые заказы создаются только в форме Sheep, убивать можно только в форме Wolf, с трупом в руках нельзя войти на кухню.

**Посетители и заказы** (`NPCAndOrder/`):
- `NpcSpawner` спавнит клиента, только если удалось зарезервировать точку заказа (`PointGroup`/`ReservablePoint`).
- `NpcCustomer` — машина состояний (`NpcState`) на `NavMeshAgent`. Взаимный обход агентов намеренно выключен (`avoidOtherCustomers`): с ним клиенты толпились во входе и у точек заказа. Не включай его обратно без решения этой проблемы. NPC не создаёт `Order` сам: он вызывает `OrderManager.RequestOrder` и ждёт коллбэк `OnOrderCreated`.
- `OrderManager` — единственный источник правды по заказам. Он хранит очередь ожидающих (`_pendingCustomers`) отдельно от активных заказов (не больше `maxActiveOrders`).
- Заказ хранит `DishFamily`, а не `ProductType`. Сопоставление — в `ProductFamily`. Неверное блюдо вызывает штраф по времени, а не провал заказа. Провал по таймеру увеличивает `MistakeCount`, при `maxMistakes` срабатывает `OnGameOver`.
- `NpcCustomer` — один `IInteractable` на три действия: выдать блюдо, убить, подобрать труп. Цвет обводки выставляется в `CanInteract`.

**Убийство и трупы.** Труп — это тот же `NpcCustomer` в состоянии `Dead`, а не `CarriableItem`. При убийстве агент отключается, а `Rigidbody2D` переводится в Kinematic: иначе тело не следует за родителем. Подобранный труп становится ребёнком `corpseHoldPoint` в `PlayerCorpseState`. Пока игрок несёт труп, `PlayerInventory` не берёт предметы. `MeatGrinderMachine` уничтожает труп и пополняет `MeatTable`.

**Паника.** `PanicManager` — синглтон (`Instance`). Значение шкалы не хранится, а каждый кадр вычисляется из состояния: если кто-то паникует, шкала равна 1, иначе берётся максимальный прогресс экспозиции. Паника возникает двумя путями: `NpcPanicZone` копит время, пока рядом игрок с трупом, а `PanicManager.ScanForWitnesses` при убийстве мгновенно пугает свидетелей в радиусе (слой `panicZoneMask`). `CallPolice()` пока заглушка.

**Геймовер** (`GameOver/`). `GameOverManager` — синглтон и единственный источник правды по состоянию «игра окончена». Это состояние внутри GameScene, а не отдельная сцена. Любая система заканчивает игру через `TriggerGameOver(GameOverReason)`. Сейчас его вызывают `OrderManager.OnGameOver` (кончились попытки, см. `RemainingAttempts`/`OnAttemptsChanged`) и `NpcCustomer.CallPolice()`. Менеджер глушит музыку, отключает управление и ставит `Time.timeScale = 0`, поэтому `GameOverUI` анимирует всё на unscaled time. Экран собирается в сцене пунктом меню **Bloody Steak/Setup Game Over** (`Assets/Scripts/Editor/GameOverSetup.cs`).

**Движение.** `PlayerMovement` двигает игрока через `Rigidbody2D.MovePosition`, ввод через legacy `Input` (Active Input Handling = Both). Он ничего не знает о других системах. Блокировку движения на время анимаций включают Animation Events через `MovementLockEvents`.

## Соглашения

- Комментарии и XML-доки пишутся на русском и часто ссылаются на пункты ТЗ («п. 9.5», «п. 13.4»). Сохраняй этот стиль.
- Связи между компонентами задаются через `[SerializeField] private`-поля в инспекторе, а события — через C# `event Action<...>`. Подписка в `OnEnable`, отписка в `OnDisable`.
- UI-компоненты (`*UI`, `*View`) только отображают состояние и не содержат игровой логики.
- **Кодировка:** все `.cs` без BOM, но часть файлов (`IInteractable.cs`, `ItemTypes.cs`, `ItemSlotView.cs`, `CameraFormSwitcher.cs`, `Player/PlayerForm.cs`, `Player/PlayerFormController.cs`) сохранена в Windows-1251, остальные в UTF-8. Перед правкой русских комментариев проверяй кодировку файла и не смешивай кодировки в одном файле.
