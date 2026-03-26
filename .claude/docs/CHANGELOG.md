# MarkPos — Changelog

_Ведёт: Analyst. Обновляется после каждой завершённой задачи._

---

## [TASK-5] - 2026-03-26
### Added
- NullFiscalClient — заглушка фискального клиента для работы без оборудования
- Флаг FiscalEnabled в конфигурации: при false используется NullFiscalClient
- Исправлен краш PayAsync при пустом теле 500-ответа от API

---

## [TASK-4] - 2026-03-26
### Added
- Экран успешной оплаты: сообщение "Какой ты молодец, возвращайся к нам ещё!!!"
- TTS (Text-to-Speech) — голосовое сообщение при успешной оплате
- Таймер обратного отсчёта 5 секунд с визуализацией conic-gradient
- Автоматический сброс сессии и переход на стартовый экран после таймера

---

## [TASK-3] - 2026-03-26
### Added
- Lottie-анимация (cash back.json) в info-strip при сканировании дисконтной карты

---

## [TASK-2] - 2026-03-26
### Added
- Endpoint GET /products/{goodsId}/image в API — читает фото товара из файловой системы
- Отображение фото товара в карточке текущего товара в Blazor UI
- Fallback на ImageCoffeCup.png при отсутствии фотографии товара

---

## [TASK-1] - 2026-03-26
### Added
- Blazor WASM проект MarkPos.BlazorUI, порт 5078
- Стартовый экран Welcome.razor
- Главный экран Main.razor с корзиной товаров
- HTTP-клиент PosApiClient для взаимодействия с API на порту 5050
- SignalR-подключение к хабу /hubs/pos для событий в реальном времени

---

## [Baseline] - до задач
### Added
- Clean Architecture: Domain / Application / Infrastructure / UI / Api
- ASP.NET Core Minimal API, порт 5050
- WPF UI (MarkPos.UI) — основной интерфейс кассы
- TCP-сканер штрихкодов, порт 60001
- MS SQL Server + Dapper для персистентности
- UseCases: AddItemByBarcode, AttachDiscountCard, CloseReceipt, RemoveItem, RequestDiscounts, SearchProducts
- Интеграция с TitanPOS (фискальный регистратор)
- Миграция на .NET 10
