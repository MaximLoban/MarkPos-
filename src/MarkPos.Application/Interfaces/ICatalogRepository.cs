namespace MarkPos.Application.Interfaces;

public interface ICatalogRepository
{
    /// <summary>Родительские группы (DeptNumber 1000–1500, у которых есть дочерние)</summary>
    Task<IReadOnlyList<GoodsGroupItem>> GetParentGroupsAsync(CancellationToken ct = default);

    /// <summary>Дочерние группы указанного родителя</summary>
    Task<IReadOnlyList<GoodsGroupItem>> GetChildGroupsAsync(long parentGroupId, CancellationToken ct = default);

    /// <summary>Есть ли у группы дочерние группы</summary>
    Task<bool> HasChildrenAsync(long groupId, CancellationToken ct = default);

    /// <summary>Товары из группы (через GoodsWeight)</summary>
    Task<IReadOnlyList<CatalogProductItem>> GetGroupItemsAsync(long groupId, CancellationToken ct = default);
}

public record GoodsGroupItem(long GoodsWeightGroupId, long DepartmentNumber, string Name);
public record CatalogProductItem(long GoodsId, string Name, decimal Price);
