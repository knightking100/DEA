using DEA.Database.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace DEA.Database.Repositories
{
    public class BaseRepository<T> where T : Model
    {
        public IMongoCollection<T> Collection { get; }

        public BaseRepository(IMongoCollection<T> collection)
        {
            Collection = collection;
        }

        /// <summary>
        /// Inserts an entity into a collection.
        /// </summary>
        /// <param name="entity">Entity in question.</param>
        public Task InsertAsync(T entity)
        {
            return Collection.InsertOneAsync(entity, null, default(CancellationToken));
        }

        /// <summary>
        /// Gets an entity based filtered through an expression.
        /// </summary>
        /// <param name="filter">Expression filter.</param>
        /// <returns>A task returing an entity.</returns>
        public Task<T> GetAsync(Expression<Func<T, bool>> filter)
        {
            return Collection.Find(filter).Limit(1).FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets a list of all entities matching an optional filter.
        /// </summary>
        /// <param name="filter">Expression filter.</param>
        /// <returns>A task returning a list of entities.</returns>
        public async Task<List<T>> AllAsync(Expression<Func<T, bool>> filter = null)
        {
            if (filter != null)
                return await (await Collection.FindAsync(filter)).ToListAsync();
            else
                return await(await Collection.FindAsync(Builders<T>.Filter.Empty)).ToListAsync();
        }

        /// <summary>
        /// Updates an entity.
        /// </summary>
        /// <param name="entity">Updated entity.</param>
        public Task UpdateAsync(T entity)
        {
            return Collection.ReplaceOneAsync(y => y.Id == entity.Id, entity);
        }
        
        /// <summary>
        /// Checks whether an entity matching a filter exists.
        /// </summary>
        /// <param name="filter">Expression filter.</param>
        /// <returns>A task returning a boolean.</returns>
        public Task<bool> ExistsAsync(Expression<Func<T, bool>> filter)
        {
            return Collection.Find(filter).Limit(1).AnyAsync();
        }

        /// <summary>
        /// Modifies an entity.
        /// </summary>
        /// <param name="entity">Entity to modify.</param>
        /// <param name="function">Modification on the entity.</param>
        public Task ModifyAsync(T entity, Action<T> function)
        {
            function(entity);
            return UpdateAsync(entity);
        }

        /// <summary>
        /// Fetches and modifies and entity.
        /// </summary>
        /// <param name="filter">Filter to find the entity.</param>
        /// <param name="function">Modification on the entity.</param>
        public async Task ModifyAsync(Expression<Func<T, bool>> filter, Action<T> function)
        {
            var entity = await GetAsync(filter);
            function(entity);
            await UpdateAsync(entity);
        }

        /// <summary>
        /// Delets an entity matching a filter.
        /// </summary>
        /// <param name="filter">Filter to find the entity.</param>
        public Task DeleteAsync(Expression<Func<T, bool>> filter)
        {
            return Collection.DeleteOneAsync(filter);
        }

    }
}
