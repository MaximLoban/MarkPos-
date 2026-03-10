using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MarkPos.Application.Scanner;
using Xunit;

namespace MarkPos.Domain.Tests;

public class ScannerParserTests
{
    private readonly ScannerParser _parser = new();

    [Fact]
    public void Parse_HttpsUrl_ReturnsAdvertisingQr()
    {
        var result = _parser.Parse("https://example.com/promo");
        Assert.IsType<AdvertisingQrMessage>(result);
    }

    [Fact]
    public void Parse_HttpUrl_ReturnsAdvertisingQr()
    {
        var result = _parser.Parse("http://example.com/promo");
        Assert.IsType<AdvertisingQrMessage>(result);
    }

    [Fact]
    public void Parse_SelfscanPrefix_ReturnsPreCheck()
    {
        var result = _parser.Parse("#SELFSCAN#123456");
        var msg = Assert.IsType<PreCheckMessage>(result);
        Assert.Equal("123456", msg.CheckNumber);
    }

    [Fact]
    public void Parse_Length17_ReturnsDiscountCard()
    {
        var result = _parser.Parse("12345678901234567");
        var msg = Assert.IsType<DiscountCardMessage>(result);
        Assert.Equal("12345678901234567", msg.CardNumber);
    }

    [Fact]
    public void Parse_Length12AllDigits_PadsZero()
    {
        var result = _parser.Parse("481000000001");
        var msg = Assert.IsType<BarcodeMessage>(result);
        Assert.Equal("0481000000001", msg.Barcode);
    }

    [Fact]
    public void Parse_Internal19_Prefix47_ReturnsBarcode()
    {
        var result = _parser.Parse("4712345678901234567");
        var msg = Assert.IsType<BarcodeMessage>(result);
        Assert.Equal("4712345678901234567", msg.Barcode);
    }

    [Fact]
    public void Parse_Internal19_Prefix49_ReturnsWeightBarcode()
    {
        var result = _parser.Parse("4912345010000234567");
        Assert.IsType<WeightBarcodeMessage>(result);
    }

    [Fact]
    public void Parse_WeightDp13_StartsWith22_ReturnsWeightBarcode()
    {
        var result = _parser.Parse("2212345010005");
        var msg = Assert.IsType<WeightBarcodeMessage>(result);
        Assert.Equal(1.000m, msg.Quantity);
    }

    [Fact]
    public void Parse_StandardEan13_ReturnsBarcode()
    {
        var result = _parser.Parse("4810000000001");
        var msg = Assert.IsType<BarcodeMessage>(result);
        Assert.Equal("4810000000001", msg.Barcode);
    }

    [Fact]
    public void Parse_EmptyString_ReturnsInvalid()
    {
        var result = _parser.Parse("");
        Assert.IsType<InvalidBarcodeMessage>(result);
    }
}