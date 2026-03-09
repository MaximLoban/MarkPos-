using MarkPos.Application.Interfaces;
using MarkPos.Domain.Entities;

namespace MarkPos.Application.UseCases;

public class SearchProductsUseCase
{
    private readonly IProductRepository _products;

    public SearchProductsUseCase(IProductRepository products)
        => _products = products;

    public Task<IReadOnlyList<Product>> ExecuteAsync(
        string query,
        CancellationToken ct = default)
        => _products.SearchAsync(query, limit: 20, ct);
}