using MarkPos.Application.Interfaces;
using MarkPos.Application.UseCases;
using MarkPos.Domain.Entities;
using MarkPos.Domain.ValueObjects;
using Moq;

namespace MarkPos.Application.Tests;

public class AddItemByBarcodeUseCaseTests
{
    private static Product MakeProduct(long id = 1) => new()
    {
        GoodsId = id,
        GoodsGroupId = 1,
        Name = $"Товар {id}",
        Price = 5.00m,
        Piece = 1
    };

    private static Mock<IReceiptRepository> MakeReceiptRepoMock()
    {
        var mock = new Mock<IReceiptRepository>();
        mock.Setup(r => r.CreateGroupAsync(It.IsAny<Receipt>(), default))
            .ReturnsAsync(1L);
        mock.Setup(r => r.InsertLineAsync(It.IsAny<long>(), It.IsAny<ReceiptLine>(), default))
            .Returns(Task.CompletedTask);
        mock.Setup(r => r.UpdateLineAsync(It.IsAny<long>(), It.IsAny<ReceiptLine>(), default))
            .Returns(Task.CompletedTask);
        return mock;
    }

    [Fact]
    public async Task Execute_ValidBarcode_AddsItemToReceipt()
    {
        var product = MakeProduct();
        var repoMock = new Mock<IProductRepository>();
        repoMock
            .Setup(r => r.FindByBarcodeAsync("4810000000001", default))
            .ReturnsAsync(product);

        var useCase = new AddItemByBarcodeUseCase(repoMock.Object, MakeReceiptRepoMock().Object);
        var receipt = Receipt.New(1);

        var result = await useCase.ExecuteAsync(receipt, "4810000000001");

        Assert.True(result.IsSuccess);
        Assert.Single(receipt.Lines);
        Assert.Equal(product.GoodsId, result.Value!.GoodsId);
    }

    [Fact]
    public async Task Execute_UnknownBarcode_Fails()
    {
        var repoMock = new Mock<IProductRepository>();
        repoMock
            .Setup(r => r.FindByBarcodeAsync(It.IsAny<string>(), default))
            .ReturnsAsync((Product?)null);

        var useCase = new AddItemByBarcodeUseCase(repoMock.Object, MakeReceiptRepoMock().Object);
        var receipt = Receipt.New(1);

        var result = await useCase.ExecuteAsync(receipt, "0000000000000");

        Assert.False(result.IsSuccess);
        Assert.Empty(receipt.Lines);
    }

    [Fact]
    public async Task Execute_EmptyBarcode_Fails()
    {
        var repoMock = new Mock<IProductRepository>();
        var useCase = new AddItemByBarcodeUseCase(repoMock.Object, MakeReceiptRepoMock().Object);
        var receipt = Receipt.New(1);

        var result = await useCase.ExecuteAsync(receipt, "");

        Assert.False(result.IsSuccess);
        repoMock.Verify(r => r.FindByBarcodeAsync(It.IsAny<string>(), default), Times.Never);
    }
}