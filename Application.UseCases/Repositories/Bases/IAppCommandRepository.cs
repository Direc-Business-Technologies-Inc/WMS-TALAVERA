using Domain.Commons;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Application.UseCases.Repositories.Bases;

public interface IAppCommandRepository
{
    DbContext GetDbContext();
    bool IsInTransaction();
    void Attach<T>(T item) where T : class;
    Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate) where T : class;
    void Update<T>(T item) where T : class;
    Task AddAsync<T>(T item) where T : class;
    Task AddManyAsync<T>(List<T> item) where T : class;
    Task DeleteManyAsync<T>(Expression<Func<T, bool>> predicate) where T : class;
    Task SaveChangesAsync();
    void SaveChanges();
    Task BeginTransactionAsync();
    Task CreateSavePointAsync(string savePointName);
    Task RollbackAsync();
    Task RollbackToAsync(string savePointName);
    Task CommitAsync();
    void MarkAsModified(object item, string property);
    void UpdateMany<T>(List<T> items) where T : class;
    void UntrackEntity<T>(T ent);
    void UntrackAll<T>(IEnumerable<T> entities) where T : class;
    void UntrackAll();
    void SetShadowProperty<T>(T entity, string propertyName, object value) where T : class;
    void SetCreateAudit<T>(T auditable) where T : AuditableDEM;
    void SetUpdateAudit<T>(T auditable) where T : AuditableDEM;
    void SetCreateAudit<T>(List<T> auditables) where T : AuditableDEM;
    void SetUpdateAudit<T>(List<T> auditables) where T : AuditableDEM;
}
