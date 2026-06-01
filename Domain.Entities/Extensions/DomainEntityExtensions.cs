using Domain.Markers;

namespace Domain.Extensions;

public static class DomainEntityExtensions
{
    public static T SetId<T>(this T entity, Guid id) where T : IDomainEntity
    {
        entity.SetId(id);
        return entity;
    }
}
