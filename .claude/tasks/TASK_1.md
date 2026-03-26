## TASK-1: Blazor WASM фронтенд — базовый UI

### Бизнес-цель
Создать новый веб-интерфейс кассы самообслуживания на Blazor WASM как замену WPF UI, доступный в браузере на порту 5078.

### Acceptance criteria
- [x] Стартовый экран (Welcome.razor) отображается при открытии
- [x] Главный экран (Main.razor) с корзиной товаров
- [x] HTTP-клиент PosApiClient подключён к API на порту 5050
- [x] SignalR-соединение с хабом /hubs/pos для получения событий в реальном времени

### Затронутые модули
- MarkPos.BlazorUI
- MarkPos.Api (PosHub)

### Статус: Done
