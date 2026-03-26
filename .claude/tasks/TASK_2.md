## TASK-2: Фото товаров в карточке текущего товара

### Бизнес-цель
Показывать фотографию товара при сканировании, чтобы покупатель мог визуально подтвердить правильность отсканированного продукта.

### Acceptance criteria
- [x] Endpoint GET /products/{goodsId}/image в API читает файл из D:\Image\GoodsImage\Goods\256\{last4}\{padded}\norm\{padded}.n_1.png
- [x] Blazor показывает фото товара при наличии отсканированного товара
- [x] Fallback на изображение-заглушку ImageCoffeCup.png если фото не найдено

### Затронутые модули
- MarkPos.Api (новый endpoint)
- MarkPos.BlazorUI (Pages/Main.razor)

### Статус: Done
