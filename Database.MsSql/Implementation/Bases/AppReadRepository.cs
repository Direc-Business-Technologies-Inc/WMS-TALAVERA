using Application.UseCases.Repositories.Bases;
using Database.MsSql.Core;
using Microsoft.EntityFrameworkCore;
using Shared.Entities;
using System.Linq.Expressions;

namespace Database.MsSql.Implementation.Bases;

public class AppReadRepository(
    AppDbContext dbContext,
    IDbContextFactory<AppDbContext> dbContextFactory)
    : IAppReadRepository
{
    public async Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate, bool local = true) where T : class
    {
        if (!local)
            return await dbContext.Set<T>()
                .AsNoTracking()
                .AnyAsync(predicate);

        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        return await ctx.Set<T>()
            .AsNoTracking()
            .AnyAsync(predicate);
    }

    public async Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, bool track = false, bool local = true) where T : class
    {
        if (!local)
        {
            var queryDI = dbContext.Set<T>()
                .AsQueryable();
            if (!track)
                queryDI = queryDI.AsNoTracking();
            return await queryDI.FirstOrDefaultAsync(predicate);
        }

        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        var query = ctx.Set<T>().AsQueryable();
        if (!track)
            query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(predicate);
    }

    public async Task<T?> SortedFirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, string? orderByPropertyName = null, bool ascending = true, bool track = false, bool local = true) where T : class
    {
        IQueryable<T> query;
        DbContext contextToUse;

        if (local)
        {
            contextToUse = dbContext;
            query = dbContext.Set<T>().AsNoTracking();
        }
        else
        {
            contextToUse = await dbContextFactory.CreateDbContextAsync();
            query = contextToUse.Set<T>().AsNoTracking();
        }

        if (predicate != null)
            query = query.Where(predicate);

        if (!string.IsNullOrWhiteSpace(orderByPropertyName))
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var entityType = contextToUse.Model.FindEntityType(typeof(T));

            if (entityType == null)
                throw new ArgumentException($"Entity type '{typeof(T).Name}' not found in model");

            var propertyParts = orderByPropertyName.Split('.');
            Expression propertyAccess = parameter;
            Type currentPropertyType = typeof(T);
            var currentEntityType = entityType;
            Microsoft.EntityFrameworkCore.Metadata.IProperty? finalProperty = null;

            foreach (var propertyName in propertyParts)
            {
                var property = currentEntityType?.FindProperty(propertyName);

                if (property != null)
                {
                    if (property.IsShadowProperty())
                    {
                        var efPropertyMethod = typeof(EF).GetMethod(nameof(EF.Property))!
                            .MakeGenericMethod(property.ClrType);
                        propertyAccess = Expression.Call(
                            efPropertyMethod,
                            propertyAccess,
                            Expression.Constant(propertyName));
                    }
                    else
                    {
                        propertyAccess = Expression.Property(propertyAccess, propertyName);
                    }
                    currentPropertyType = property.ClrType;
                    finalProperty = property;
                }
                else
                {
                    var navigation = currentEntityType?.FindNavigation(propertyName);

                    if (navigation != null && navigation.TargetEntityType.IsOwned())
                    {
                        propertyAccess = Expression.Property(propertyAccess, propertyName);
                        currentPropertyType = navigation.ClrType;
                        currentEntityType = navigation.TargetEntityType;
                    }
                    else
                    {
                        throw new ArgumentException(
                            $"Property or navigation '{propertyName}' not found on type '{currentEntityType?.ClrType.Name ?? "Unknown"}'");
                    }
                }
            }

            if (finalProperty == null)
                throw new ArgumentException($"Invalid property path '{orderByPropertyName}'");

            var lambda = Expression.Lambda(propertyAccess, parameter);

            var orderMethod = ascending ? "OrderBy" : "OrderByDescending";
            var resultExpression = Expression.Call(
                typeof(Queryable),
                orderMethod,
                new[] { typeof(T), currentPropertyType },
                query.Expression,
                lambda);

            query = query.Provider.CreateQuery<T>(resultExpression);
        }

        var result = await query.FirstOrDefaultAsync();

        if (!local && contextToUse is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }

        return result;

    }

    public async Task<List<T>> GetAllAsync<T>(bool track = false, bool local = true) where T : class
    {
        if (!local)
        {
            var queryDI = dbContext.Set<T>()
                .AsQueryable();
            if (!track)
                queryDI = queryDI.AsNoTracking();
            return await queryDI.ToListAsync();
        }

        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        var query = ctx.Set<T>().AsQueryable();
        if (!track)
            query.AsNoTracking();
        return await query.ToListAsync();
    }

    public async Task<List<T>> GetAllWithIntentAsync<T>(DataGridIntent intent, bool track = false, bool local = true) where T : class
    {

        if (!local)
        {
            var queryDI = dbContext.Set<T>()
                .AsQueryable();

            if (!track)
                queryDI = queryDI.AsNoTracking();
            return await queryDI.ToListAsync();
        }

        await using var ctx = await dbContextFactory.CreateDbContextAsync();

        var query = ctx.Set<T>().AsQueryable();

        if (!track)
            query.AsNoTracking();
        return await query.ToListAsync();
    }

    public async Task<int> GetCountAsync<T>(Expression<Func<T, bool>> predicate, bool local = true) where T : class
    {
        if (!local)
            return await dbContext.Set<T>()
                .AsNoTracking()
                .CountAsync(predicate);

        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        return await ctx.Set<T>().CountAsync(predicate);
    }

    public async Task<List<T>> GetListAsync<T>(Expression<Func<T, bool>> predicate, bool track = false, bool local = true) where T : class
    {
        if (!local)
        {
            var queryDI = dbContext.Set<T>()
                .AsQueryable();
            if (!track)
                queryDI = queryDI.AsNoTracking();
            return await queryDI.Where(predicate).ToListAsync();

        }

        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        var query = ctx.Set<T>().AsQueryable();
        if (!track)
            query = query.AsNoTracking();
        return await query.Where(predicate).ToListAsync();
    }

    public async Task<List<TProjection>> GetManyProjectedAsync<TEntity, TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> selector, bool local = true) where TEntity : class
    {
        if (!local)
            return await dbContext.Set<TEntity>()
                .AsNoTracking()
                .Where(predicate)
                .Select(selector)
                .ToListAsync();

        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        return await ctx.Set<TEntity>()
            .AsNoTracking()
            .Where(predicate)
            .Select(selector)
            .ToListAsync();
    }

    public async Task<List<TProjection>> GetManyProjectedWithIntentAsync<TEntity, TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> selector, DataGridIntent intent, bool local = true) where TEntity : class
    {
        if (!local)
            return await dbContext.Set<TEntity>()
                .AsNoTracking()
                .Where(predicate)
                .Select(selector)
                .ToListAsync();

        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        return await ctx.Set<TEntity>()
            .AsNoTracking()
            .Where(predicate)
            .Select(selector)
            .ToListAsync();
    }

    public async Task<TProjection?> GetProjectedAsync<TEntity, TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> selector, bool local = true) where TEntity : class
    {
        if (!local)
        {
            return await dbContext.Set<TEntity>()
                .AsNoTracking()
                .Where(predicate)
                .Select(selector)
                .FirstOrDefaultAsync();
        }

        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        return await ctx.Set<TEntity>()
            .AsNoTracking()
            .Where(predicate)
            .Select(selector)
            .FirstOrDefaultAsync();
    }

    public async Task<TProjection?> GetProjectedWithIntentAsync<TEntity, TProjection>(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TProjection>> selector, DataGridIntent intent, bool local = true) where TEntity : class
    {
        if (!local)
        {
            return await dbContext.Set<TEntity>()
                .AsNoTracking()
                .Where(predicate)
                .Select(selector)
                .FirstOrDefaultAsync();
        }

        await using var ctx = await dbContextFactory.CreateDbContextAsync();
        return await ctx.Set<TEntity>()
            .AsNoTracking()
            .Where(predicate)
            .Select(selector)
            .FirstOrDefaultAsync();
    }
}
