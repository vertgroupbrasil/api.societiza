using Core.Domain.Primitives.Records;

namespace Core.Domain.Primitives.Interfaces;

public interface IPagedResult<out TObject>
{
    IReadOnlyCollection<TObject> Items { get; }
    Page Page { get; }
}