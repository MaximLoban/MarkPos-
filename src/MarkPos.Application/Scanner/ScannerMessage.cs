using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarkPos.Application.Scanner;

/// <summary>
/// Результат разбора строки от сканера.
/// </summary>
public abstract record ScannerMessage;

/// <summary>Обычный штрихкод — искать товар.</summary>
public record BarcodeMessage(string Barcode) : ScannerMessage;

/// <summary>Весовой штрихкод с уже закодированным количеством.</summary>
public record WeightBarcodeMessage(string Barcode, decimal Quantity) : ScannerMessage;

/// <summary>Дисконтная карта — длина 17.</summary>
public record DiscountCardMessage(string CardNumber) : ScannerMessage;

/// <summary>QR содержит рекламную ссылку.</summary>
public record AdvertisingQrMessage : ScannerMessage;

/// <summary>Предчек (#SELFSCAN#).</summary>
public record PreCheckMessage(string CheckNumber) : ScannerMessage;

/// <summary>Неверный штрихкод — ни один алгоритм не сработал.</summary>
public record InvalidBarcodeMessage(string Raw, string Reason) : ScannerMessage;