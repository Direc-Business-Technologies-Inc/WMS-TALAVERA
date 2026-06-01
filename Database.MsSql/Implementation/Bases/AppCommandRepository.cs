using Application.UseCases.Repositories.Bases;
using Database.MsSql.Core;
using Domain.Commons;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Services.Repository;
using System.Linq.Expressions;

namespace Database.MsSql.Implementation.Bases;

public class AppCommandRepository(
    AppDbContext context,
    ICurrentUserService currentUser)
    : IAppCommandRepository
{

    IDbContextTransaction? transaction;

    public async Task AddAsync<T>(T item) where T : class
    {
        try
        {
            if (item is AuditableDEM auditable)
            {
                SetCreateAudit(auditable);
            }
            await context.Set<T>().AddAsync(item);
            await context.SaveChangesAsync();

        }
        catch (Exception ex)
        {
            throw new Exception(
                $"Error adding entity async of type {typeof(T).Name}. Message: {ex.Message}",
                ex
            );
        }
    }

    public async Task AddManyAsync<T>(List<T> items) where T : class
    {
        try
        {
            if (items is List<AuditableDEM> auditables)
            {
                SetCreateAudit(auditables);
            }
            await context.AddRangeAsync(items);
            await context.SaveChangesAsync();

        }
        catch (Exception ex)
        {
            throw new Exception(
                $"Error adding many entities of type {typeof(T).Name}. Message: {ex.Message}",
                ex
            );
        }
    }

    public void Attach<T>(T item) where T : class
    {
        context.Attach<T>(item);
    }

    public async Task BeginTransactionAsync()
    {
        transaction = await context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        try
        {
            if (transaction is not null)
            {
                await transaction.CommitAsync();
                await transaction.DisposeAsync();
                transaction = null;
                context.ChangeTracker.Clear();

            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error during transaction commit", ex);
        }
    }

    public async Task CreateSavePointAsync(string savePointName)
    {
        try
        {
            if (transaction is not null)
            {
                await transaction.CreateSavepointAsync(savePointName);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error creating save point in transaction", ex);
        }
    }

    public async Task DeleteManyAsync<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        try
        {
            await context.Set<T>().Where(predicate).ExecuteDeleteAsync();
        }
        catch (Exception e)
        {
            throw new Exception(
                $"Error deleting entities of type {typeof(T).Name}. Message: {e.Message}",
                e
            );
        }
    }

    public async Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        var query = context.Set<T>().AsQueryable();
        return await query.FirstAsync(predicate);
    }

    public DbContext GetDbContext() => context;

    public bool IsInTransaction()
    {
        return context.Database.CurrentTransaction is not null;
    }

    public void MarkAsModified(object item, string property)
    {
        EntityEntry entity = context.Entry(item);
        entity.State = EntityState.Modified;
    }

    public async Task RollbackAsync()
    {
        try
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync();
                await transaction.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error during transaction rollback", ex);
        }
    }

    public async Task RollbackToAsync(string savePointName)
    {
        try
        {
            if (transaction is not null)
            {
                await transaction.RollbackToSavepointAsync(savePointName);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error rolling back to a save point", ex);
        }
    }

    public void SaveChanges()
    {
        try
        {
            context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception("Error saving changes", ex);
        }
    }

    public async Task SaveChangesAsync()
    {
        try
        {
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new Exception("Error saving changes async.", ex);
        }
    }

    public void SetCreateAudit<T>(T auditable) where T : AuditableDEM
    {
        auditable.SetCreatedBy(currentUser.UserId);
    }

    public void SetCreateAudit<T>(List<T> auditables) where T : AuditableDEM
    {
        foreach (var aud in auditables)
        {
            aud.SetCreatedBy(currentUser.UserId);
        }
    }

    public void SetShadowProperty<T>(T entity, string propertyName, object value) where T : class
    {
        context.Entry(entity).Property(propertyName).CurrentValue = value;
    }

    public void SetUpdateAudit<T>(T auditable) where T : AuditableDEM
    {
        auditable.SetUpdatedBy(currentUser.UserId);
    }

    public void SetUpdateAudit<T>(List<T> auditables) where T : AuditableDEM
    {
        foreach (var aud in auditables)
        {
            aud.SetUpdatedBy(currentUser.UserId);
        }
    }

    public void UntrackAll<T>(IEnumerable<T> entities) where T : class
    {
        foreach (var entity in entities)
        {
            context.Entry(entity).State = EntityState.Detached;
        }
    }

    public void UntrackAll()
    {
        context.ChangeTracker.Clear();
    }

    public void UntrackEntity<T>(T ent)
    {
        context.Entry(ent).State = EntityState.Detached;
    }

    public void Update<T>(T item) where T : class
    {
        try
        {
            if (item is AuditableDEM auditable)
            {
                SetUpdateAudit(auditable);
            }

            context.Update(item);
            context.SaveChanges();

        }
        catch (Exception ex)
        {
            throw new Exception(
                $"Error updating entity of type {typeof(T).Name}. Message: {ex.Message}",
                ex
            );
        }
    }

    public void UpdateMany<T>(List<T> items) where T : class
    {
        try
        {
            if (items is List<AuditableDEM> auditables)
            {
                SetUpdateAudit(auditables);
            }

            context.UpdateRange(items);
            context.SaveChanges();

        }
        catch (Exception e)
        {
            throw new Exception(
                $"Error updating entities of type {typeof(T).Name}. Message: {e.Message}",
                e
            );
        }
    }
}
