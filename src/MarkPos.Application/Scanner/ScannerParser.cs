using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;

namespace MarkPos.Application.Scanner;

/// <summary>
/// Парсер строк от сканера. Реализует алгоритм из документа "Обработка ШК и QR в MPS4".
/// Чистая логика без зависимостей — легко тестируется.
/// </summary>
public class ScannerParser
{
    /// <summary>Разобрать строку полученную от сканера.</summary>
    public ScannerMessage Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new InvalidBarcodeMessage(raw, "Пустая строка");

        var input = raw.Trim();

        // 1. Проверка на рекламный QR (ссылка)
        if (input.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            input.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return new AdvertisingQrMessage();

        // 2. Предчек #SELFSCAN#
        if (input.Contains("#SELFSCAN#", StringComparison.OrdinalIgnoreCase))
        {
            var clean = input.Replace("#SELFSCAN#", "", StringComparison.OrdinalIgnoreCase);
            if (clean.Length >= 6)
                return new PreCheckMessage(clean[..6]);
            return new InvalidBarcodeMessage(raw, "Некорректный предчек");
        }

        // 3. Дисконтная карта — длина 17
        if (input.Length == 17)
            return new DiscountCardMessage(input);

        // 4. Внутренние ШК ЕТ — длина 19
        if (input.Length == 19)
            return ParseInternal19(input);

        // 5. Весовой ШК от весов DP — длина 13, первые 2 символа "22"
        if (input.Length == 13 && input.StartsWith("22"))
            return ParseWeightDp13(input);

        // 6. Весовой ШК от весов DP с алармированием — длина 18, первые 2 символа "22"
        if (input.Length == 18 && input.StartsWith("22"))
            return ParseWeightDp18(input);

        // 7. Длина 12 — сканер отрезал первый 0 в EAN-13
        if (input.Length == 12 && input.All(char.IsDigit))
            return new BarcodeMessage("0" + input);

        // 8. Стандартный поиск по Barcodes
        var barcode = StripNonDigits(input);
        if (!string.IsNullOrEmpty(barcode))
            return new BarcodeMessage(barcode);

        return new InvalidBarcodeMessage(raw, "Неверный штрих-код");
    }

    // ── Внутренние форматы ЕТ ────────────────────────────────────────────────

    private static ScannerMessage ParseInternal19(string input)
    {
        var prefix = input[..2];
        return prefix switch
        {
            "47" => new BarcodeMessage(input),      // маркировка штучных
            "49" => new WeightBarcodeMessage(input, ParseWeightFromInternal(input)), // весовых
            "44" => ParsePrice44(input),             // товар с ценой
            "77" => new WeightBarcodeMessage(input, ParseWeightCas(input)), // весы CAS
            _ => new BarcodeMessage(input)
        };
    }

    /// <summary>ШК 44XXXXXPPPPPC — товар с ценой, символы 8-12 = цена в копейках.</summary>
    private static ScannerMessage ParsePrice44(string input)
    {
        // Извлекаем GoodsId (символы 2-7) и цену (символы 7-12)
        var goodsPart = input[2..7];
        var pricePart = input[7..12];

        if (decimal.TryParse(pricePart, out var priceKopecks))
            return new BarcodeMessage(input); // Поиск по полному ШК, цена применится в репозитории

        return new BarcodeMessage(input);
    }

    /// <summary>Весовой ДП 13 символов: "22" + 5 цифр товара + 5 цифр веса + контрольная.</summary>
    private static ScannerMessage ParseWeightDp13(string input)
    {
        if (!input.All(char.IsDigit))
            return new BarcodeMessage(input);

        // Символы 2-6: код товара, 7-11: вес в граммах
        var weightGrams = input.Substring(7, 5);
        if (decimal.TryParse(weightGrams, out var grams))
            return new WeightBarcodeMessage(input[..7], grams / 1000m);

        return new BarcodeMessage(input);
    }

    /// <summary>Весовой ДП 18 символов с алармированием.</summary>
    private static ScannerMessage ParseWeightDp18(string input)
    {
        if (!input.All(char.IsDigit))
            return new BarcodeMessage(input);

        var weightGrams = input.Substring(7, 5);
        if (decimal.TryParse(weightGrams, out var grams))
            return new WeightBarcodeMessage(input[..7], grams / 1000m);

        return new BarcodeMessage(input);
    }

    private static decimal ParseWeightFromInternal(string input)
    {
        // Символы 8-12: вес в граммах
        if (input.Length >= 13 && decimal.TryParse(input.Substring(8, 5), out var grams))
            return grams / 1000m;
        return 1m;
    }

    private static decimal ParseWeightCas(string input)
    {
        // CAS весы: символы 7-11 = вес
        if (input.Length >= 12 && decimal.TryParse(input.Substring(7, 5), out var grams))
            return grams / 1000m;
        return 1m;
    }

    private static string StripNonDigits(string input)
        => new string(input.Where(char.IsDigit).ToArray());
}