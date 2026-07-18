# Архитектурные правила UralGameJam2026

Этот файл фиксирует принятые в проекте архитектурные решения. Перед изменением кода нужно сверяться с ним. Если новое пожелание пользователя противоречит этому файлу, приоритет имеет последнее явное пожелание пользователя, после чего правило следует обновить здесь.

## Общий подход

- Игровая логика строится на Unity Entities (`SystemBase`, `IJobEntity`, компоненты ECS).
- Связь с `GameObject`, `Animator`, `Transform`, `CharacterController`, UI и другими managed-объектами хранится в managed `IComponentData` view-компонентах.
- Системы, работающие с UnityEngine-объектами, выполняются синхронно через `Run()` в `PresentationSystemGroup`.
- Состояние и одноразовые события должны быть явно разделены: состояния могут быть enableable-компонентами, события оформляются request-компонентами по факту их наличия.
- Не делать предварительные `HasComponent`-проверки перед `AddComponent` или `RemoveComponent`: в принятом стиле эти операции используются напрямую и считаются идемпотентными. `HasComponent` оставлять только там, где от наличия компонента действительно ветвится логика либо перед операцией, которая требует существования компонента.
- Не добавлять защитные проверки против состояния, которое уже исключено архитектурным инвариантом потока данных. Если request создаётся только после записи валидных данных, обработчик request не проверяет повторно, были ли эти данные записаны.
- Обязательные Unity-ссылки view-компонента проверяются один раз в `Awake` MonoBehaviour до создания ECS-компонентов. После успешной инициализации ECS-системы считают эти ссылки гарантированно ненулевыми и не повторяют null-проверки.
- Null-проверки в системах оставлять только для явно необязательных ссылок.
- При перемещении объекта с временно выключенным `CharacterController` сначала установить конечную позицию и только затем включать контроллер. Включение внутри trigger до телепорта может повторно вызвать `OnTriggerEnter` и создать новый request.

## Организация подсистем игрока

- ECS-компоненты подсистем игрока хранятся в `PlayerComponents.cs` и группируются через `#region` по системе-владельцу.
- Таймер жизни игрока относится к подсистеме смерти: настройки и UI-ссылка находятся в `Player` на вкладке `LifeTime`, логика — в `PlayerDeathSystem`, компоненты — в регионе `PlayerDeathSystem`.
- Настройки blend shape, зависящие от таймера жизни, находятся в `Player` на вкладке `LifeTime`, логика — в `PlayerBlendShapeSystem`; отдельный `BlendShapeController` не используется.

## Обработка физических событий

- `PhysicsEventTriggerComponent` и `PhysicsEventCollideComponent` являются общими буферами: MonoBehaviour callbacks могут дописывать в них события в любой момент кадра.
- Архитектурный инвариант: `OnTriggerEnter`, `OnTriggerExit`, `OnCollisionEnter` и `OnCollisionExit` не вызываются во время выполнения систем `PresentationSystemGroup`.
- Системы-потребители читают общие физические буферы полностью и не изменяют их; `PhysicsSystem` выполняется последним в `PresentationSystemGroup` и единолично очищает буферы.
- Каждая игровая фича фильтрует и обрабатывает нужные ей физические события в своей системе-владельце; `PhysicsSystem` игровую реакцию на коллизии не выполняет.
- Во время обхода общего буфера не выполнять structural changes: сначала закончить чтение и сохранить найденные сущности, затем менять их компоненты.

## Request-компоненты

- Все компоненты с `Request` в названии являются обычными `IComponentData` и не реализуют `IEnableableComponent`.
- Создание запроса: `AddComponent<RequestType>(entity)`.
- Обработка запроса: job/query выбирает сущность по наличию `RequestType`.
- После обработки request-компонент удаляется через `RemoveComponent<RequestType>(entity)`.
- Не хранить request-компоненты на сущности в выключенном состоянии.
- Не использовать для request-компонентов `SetComponentEnabled`, `EnabledRefRO`, `EnabledRefRW`, `WithDisabled` или `IgnoreComponentEnabledState`.
- Animation Event вызывает метод MonoBehaviour, а тот только добавляет соответствующий request-компонент. Основная логика остаётся в ECS-системе.

Текущие request-компоненты:

- `PlayerClimbRequestTag`
- `PlayerFinishClimbRequest`
- `PlayerFinishRespawnRequest`
- `RestartFireRequestTag`
- `PlayerDeathRequest`

## Структурные изменения из job

- Нельзя напрямую менять архетип сущности внутри `IJobEntity`.
- Для добавления и удаления компонентов из job использовать `EntityCommandBuffer`.
- Получать `EntityCommandBuffer` через singleton подходящей ECB-системы, выбранной по update group и требуемому моменту playback.
- Для систем из `PresentationSystemGroup` в текущем контексте подходит `BeginPresentationEntityCommandBufferSystem.Singleton`:

```csharp
var ecbSingleton = SystemAPI.GetSingleton<BeginPresentationEntityCommandBufferSystem.Singleton>();
var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);
```

- Не создавать `EntityCommandBuffer` вручную через `new` и не вызывать для него вручную `Playback` или `Dispose`.
- Не использовать `BeginPresentationEntityCommandBufferSystem` механически в системах из других групп: выбирать соответствующую ECB-систему (`Begin...`/`End...`) с учётом порядка выполнения.
- Локальная переменная `EntityCommandBuffer` всегда называется `ecb`.
- Поле `EntityCommandBuffer`, передаваемое в job, всегда называется `ecb`.
- Поле `ecb` всегда объявляется первым среди полей job.
- В object initializer при передаче данных в job присваивание `ecb = ecb` всегда указывается первым.

## Read-only поля job

- Все поля job, которые внутри job только читаются, помечаются атрибутом `[ReadOnly]`.
- Правило распространяется на входные компоненты, lookup-поля, native-контейнеры и скалярные параметры.
- Поля, через которые job записывает данные или команды, включая `EntityCommandBuffer`, атрибутом `[ReadOnly]` не помечаются.

## Именование lookup-полей

- Переменные и поля, полученные через `SystemAPI.GetComponentLookup<T>()`, именуются по шаблону `<componentName>Lookup`.
- Сначала указывается смысловое имя компонента в `camelCase`, затем суффикс `Lookup`.
- Технические суффиксы типа компонента `Component` и `Tag` в имени lookup обычно опускаются.
- Пример: `ComponentLookup<ClimbComponent>` называется `climbLookup`.

## Сигнатура IJobEntity.Execute

- Если job использует сущность, параметр `Entity entity` всегда указывается первым в сигнатуре `Execute`.
- Остальные компоненты и буферы перечисляются после него.

```csharp
public void Execute(Entity entity, ref PlayerClimbTargetComponent climbTarget,
    PlayerViewComponent view)
```

## Enableable-компоненты

- `IEnableableComponent` используется для продолжительных состояний, которые часто переключаются без изменения архетипа.
- Примеры: блокировка движения/камеры, пауза lifetime, смерть, активный процесс restart.
- Параметр `EnabledRefRO/RW<T>` влияет на автоматически сгенерированную query. Всегда учитывать enabled-состояние каждого такого компонента.
- Если job должна работать с выключенным состоянием, задавать это явно через `WithDisabled` либо использовать `IgnoreComponentEnabledState` с явной проверкой внутри job.
- Не добавлять `IgnoreComponentEnabledState` без явной необходимости: он отключает фильтрацию для всех enableable-компонентов query.
- Job, обрабатывающая обязательный request завершения процесса, не должна отфильтровываться из-за текущего enabled-состояния компонентов, которые она обязана привести в конечное состояние. Для такой job допустим `IgnoreComponentEnabledState`, а итоговые enabled-значения задаются внутри `Execute` явно.

## Проверка изменений

- После изменения C#-кода запускать:

```powershell
dotnet build .\Assembly-CSharp.csproj --no-restore -v:minimal
```

- Успешная обычная .NET-сборка не заменяет Play Mode-проверку поведения Unity, animation events, порядка систем и generated ECS queries.
- Не исправлять несвязанные предупреждения и пользовательские изменения без отдельного запроса.

## Как дополнять этот файл

- Новое архитектурное решение записывать коротким проверяемым правилом.
- Фиксировать не конкретный баг, а общий принцип, предотвращающий его повторение.
- Если правило устарело, изменять существующий пункт, а не добавлять противоречащий ему новый.
