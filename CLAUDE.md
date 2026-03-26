# MarkPos — правила для всех агентов

## Проект
Касса самообслуживания. .NET 10, WPF (основной UI) + Blazor WASM (новый фронт).
GitHub: https://github.com/MaximLoban/MarkPos-.git
Корень: D:\NewPos\MarkPos\

## Архитектура: Clean Architecture (строго)
Направление зависимостей: Domain → Application → Infrastructure → UI/API

- **Domain** (`MarkPos.Domain`): сущности Receipt, ReceiptLine, Product, DiscountCard.
  NO зависимостей от других слоёв, только C# primitives.
- **Application** (`MarkPos.Application`): UseCase, IPosSession, интерфейсы.
  Зависит ТОЛЬКО от Domain.
- **Infrastructure** (`MarkPos.Infrastructure`): TitanPosHttpClient, Dapper-репозитории, TcpScanner.
  Реализует интерфейсы Application.
- **UI/API** (`MarkPos.UI`, `MarkPos.Api`, `MarkPos.BlazorUI`):
  Зависит от Application через DI. Никогда не ссылается на Infrastructure напрямую.

## ЗАПРЕЩЕНО — архитектурные нарушения
- Infrastructure напрямую в UI/Blazor — СТОП
- DbContext / Dapper в Domain — СТОП
- Бизнес-логика в .razor / ViewModel — СТОП
- UseCase создаёт HTTP-запросы напрямую — СТОП
- EF Core — СТОП (только Dapper)

## MVVM
- ViewModel: не знает о View, только ICommand + observable свойства
- Blazor: логика в @code вызывает только PosApiClient, не HTTP напрямую
- Никакой бизнес-логики в code-behind

## Стек
- .NET 10
- WPF (основной UI) + Blazor WASM (новый фронт)
- ASP.NET Core Minimal API — порт 5050
- Blazor WASM — порт 5078
- SignalR hub: /hubs/pos
- MS SQL Server + Dapper
- TCP-сканер: порт 60001
- FiscalEnabled: false (тесты без железа)

## Ключевые файлы (неочевидные точки входа)
- DI и запуск API: `src/MarkPos.Api/Program.cs`
- Конфиг (сканер, fiscal): `src/MarkPos.Api/appsettings.json`
- SignalR hub: `src/MarkPos.Api/Hubs/PosHub.cs`
- Главный HTTP-клиент Blazor: `src/MarkPos.BlazorUI/Services/PosApiClient.cs`
- Стартовый экран: `src/MarkPos.BlazorUI/Pages/Welcome.razor`
- Главный экран с корзиной: `src/MarkPos.BlazorUI/Pages/Main.razor`

## DI-правила
- `IPosSession` — Scoped
- `PosStateNotifier` — Singleton, доступ к Scoped через `IServiceProvider.CreateScope()`
- Новые сервисы Infrastructure регистрировать в `Program.cs` через extension methods

## Запуск
```
# Терминал 1 — API
dotnet run --project src\MarkPos.Api\MarkPos.Api.csproj

# Терминал 2 — Blazor
dotnet run --project src\MarkPos.BlazorUI\MarkPos.BlazorUI.csproj
```

## Проверка после каждого изменения
```
dotnet build src/MarkPos.sln
```
Ноль ошибок, ноль warnings уровня error = минимальная планка.

## Структура проекта
Актуальная карта файлов: см. `STRUCTURE.md` (генерируется скриптом `update-structure.ps1`)

## Стиль работы агентов
- Без объяснений если не просят явно
- Только код + результат билда
- После каждого изменения — dotnet build
- /clear между несвязанными задачами