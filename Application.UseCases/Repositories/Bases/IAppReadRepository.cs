using Shared.Entities;
using System.Linq.Expressions;

namespace Application.UseCases.Repositories.Bases;

public interface IAppReadRepository
{
    Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, bool track = false, bool local = true)
        where T : class;

    Task<T?> SortedFirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, string? orderByPropertyName = null, bool ascending = true, bool track = false, bool local = true)
        where T : class;

    Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> predicate, bool track = false, bool local = true)
        where T : class;

    Task<List<T>> GetAllAsync<T>(bool track = false, bool local = true)
        where T : class;

    Task<List<T>> GetAllWithIntentAsync<T>(DataGridIntent intent, bool track = false, bool local = true)
        where T : class;

    Task<int> GetCountAsync<T>(Expression<Func<T, bool>> predicate, bool local = true)
        where T : class;

    Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate, bool local = true)
        where T : class;

    Task<List<TProjection>> GetManyProjectedAsync<TEntity, TProjection>(
       Expression<Func<TEntity, bool>> predicate,
       Expression<Func<TEntity, TProjection>> selector,
       bool local = true)
       where TEntity : class;

    Task<List<TProjection>> GetManyProjectedWithIntentAsync<TEntity, TProjection>(
       Expression<Func<TEntity, bool>> predicate,
       Expression<Func<TEntity, TProjection>> selector,
       DataGridIntent intent,
       bool local = true)
       where TEntity : class;

    Task<TProjection?> GetProjectedAsync<TEntity, TProjection>(
       Expression<Func<TEntity, bool>> predicate,
       Expression<Func<TEntity, TProjection>> selector,
       bool local = true)
        where TEntity : class;
    Task<TProjection?> GetProjectedWithIntentAsync<TEntity, TProjection>(
       Expression<Func<TEntity, bool>> predicate,
       Expression<Func<TEntity, TProjection>> selector,
       DataGridIntent intent,
       bool local = true)
        where TEntity : class;
}
