## TASK-5: NullFiscalClient — работа без фискального оборудования

### Бизнес-цель
Обеспечить возможность запуска и тестирования кассы без подключённого фискального оборудования (TitanPOS).

### Acceptance criteria
- [x] Флаг FiscalEnabled в конфигурации (appsettings.json)
- [x] При FiscalEnabled=false используется NullFiscalClient (заглушка)
- [x] При FiscalEnabled=true используется реальный TitanPosHttpClient
- [x] Исправлен краш PayAsync при пустом теле 500-ответа от фискального сервиса

### Затронутые модули
- MarkPos.Infrastructure (NullFiscalClient.cs)
- MarkPos.Api (Program.cs, appsettings.json)
- MarkPos.Application (CloseReceiptUseCase)

### Статус: Done
