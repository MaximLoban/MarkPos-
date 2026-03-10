using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkPos.Application;

/// <summary>
/// Конфигурация кассы — читается из ConstList в БД или из appsettings.
/// </summary>
public class StationConfig
{
    public string ShopNumber { get; init; } = string.Empty;
    public string StationNumber { get; init; } = string.Empty;
    public string FiscalType { get; init; } = "81";
    public string CashboxType { get; init; } = "3";
    public string StationSaleTypeId { get; init; } = "18";
	public string BaseId { get; init; } = "1";
}