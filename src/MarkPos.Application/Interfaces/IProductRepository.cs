using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Domain.Entities;

namespace MarkPos.Application.Interfaces;

public interface IProductRepository
{
    /// <summary>
    /// Поиск по штрихкоду.
    /// JOIN Goods + Barcodes WHERE Barcodes.Barcode = @barcode
    /// </summary>
    Task<Product?> FindByBarcodeAsync(string barcode, CancellationToken ct = default);

    /// <summary>
    /// Поиск по названию для UI-каталога.
    /// WHERE GoodsName LIKE @query
    /// </summary>
    Task<IReadOnlyList<Product>> SearchAsync(string query, int limit = 20, CancellationToken ct = default);
}