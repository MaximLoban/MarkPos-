using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkPos.Domain.Entities;

/// <summary>
/// Товар из таблицы Goods + Barcodes.
/// </summary>
public class Product
{
    public long GoodsId { get; init; }
    public long GoodsGroupId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string? Barcode { get; init; }
    public string? Gtin { get; init; }
    public decimal? BarcodeQuantity { get; init; }  // Количество из Barcodes.Quantity
    public long? DiscountGroupId { get; init; }
    public decimal? GoodsMinQuantity { get; init; }
    public long? Piece { get; init; }               // 1 = штучный
    public long? GoodsTypeForFiscal { get; init; }
    public short? MarkingType { get; init; }        // Маркированный товар

    /// <summary>
    /// Тип кода для TitanPOS:
    /// 0 = без кода, 1 = EAN13, 3 = услуга, 4 = авансовый платёж
    /// </summary>
    public int TitanCodeType => GoodsTypeForFiscal switch
    {
        3 => 3,
        4 => 4,
        _ => Gtin != null ? 1 : 0
    };

    /// <summary>Весовой товар — количество вводится вручную или с весов.</summary>
    public bool IsWeighted => Piece != 1;
}