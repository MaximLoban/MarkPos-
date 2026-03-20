# MarkPos — Касса самообслуживания

.NET 8 / WPF + ASP.NET Core Minimal API

## Требования
- .NET 8 SDK
- MS SQL Server
- TitanPOS (локально)

## Настройка
1. `cp src/MarkPos.UI/appsettings.json.example src/MarkPos.UI/appsettings.json`
2. Заполни строки подключения к БД и URL сервисов в `appsettings.json`


## Запуск кассы
dotnet run --project src/MarkPos.UI

## Запуск REST API
dotnet run --project src/MarkPos.Api
Swagger: http://localhost:5050

## Архитектура
Clean Architecture: Domain → Application → Infrastructure → UI/Api
Подробнее: см. документ MarkPos_Architecture_v2.docx
