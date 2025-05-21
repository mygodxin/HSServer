using MongoDB.Driver;

namespace Core
{
    public class MongoDbManager<T> where T : class
    {
        private readonly IMongoCollection<T> _collection;
        private readonly List<T> _pending = new List<T>();
        private readonly Timer _saveTimer;
        private bool _disposed;

        public MongoDbManager(string connStr, string dbName, string collName, int saveInterval = 60)
        {
            var client = new MongoClient(connStr);
            var db = client.GetDatabase(dbName);
            _collection = db.GetCollection<T>(collName);

            if (saveInterval > 0)
            {
                _saveTimer = new Timer(SaveCallback, null,
                    TimeSpan.FromSeconds(saveInterval),
                    TimeSpan.FromSeconds(saveInterval));
            }
        }

        #region 基本操作
        public async Task AddAsync(T doc, CancellationToken ct = default)
        {
            await _collection.InsertOneAsync(doc, cancellationToken: ct);
        }

        public async Task AddManyAsync(IEnumerable<T> docs, CancellationToken ct = default)
        {
            await _collection.InsertManyAsync(docs, cancellationToken: ct);
        }

        public async Task<T> GetAsync(FilterDefinition<T> filter, CancellationToken ct = default)
        {
            return await _collection.Find(filter).FirstOrDefaultAsync(ct);
        }

        public async Task<List<T>> GetAllAsync(FilterDefinition<T> filter, CancellationToken ct = default)
        {
            return await _collection.Find(filter).ToListAsync(ct);
        }

        public async Task<long> UpdateAsync(FilterDefinition<T> filter, UpdateDefinition<T> update, CancellationToken ct = default)
        {
            var result = await _collection.UpdateOneAsync(filter, update, cancellationToken: ct);
            return result.ModifiedCount;
        }

        public async Task<long> ReplaceAsync(FilterDefinition<T> filter, T replacement, CancellationToken ct = default)
        {
            var result = await _collection.ReplaceOneAsync(filter, replacement, cancellationToken: ct);
            return result.ModifiedCount;
        }

        public async Task<long> RemoveAsync(FilterDefinition<T> filter, CancellationToken ct = default)
        {
            var result = await _collection.DeleteOneAsync(filter, cancellationToken: ct);
            return result.DeletedCount;
        }
        #endregion

        #region 批量操作
        public void Queue(T doc)
        {
            lock (_pending)
            {
                _pending.Add(doc);
            }
        }

        public async Task<int> SaveAsync(CancellationToken ct = default)
        {
            List<T> toSave;
            lock (_pending)
            {
                if (_pending.Count == 0) return 0;
                toSave = new List<T>(_pending);
                _pending.Clear();
            }

            await _collection.InsertManyAsync(toSave, cancellationToken: ct);
            return toSave.Count;
        }

        private async void SaveCallback(object state)
        {
            if (_pending.Count > 0)
            {
                try
                {
                    var count = await SaveAsync();
                    Console.WriteLine($"自动保存: {count} 条记录");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"保存失败: {ex.Message}");
                }
            }
        }
        #endregion

        #region 索引管理
        public async Task<string> CreateIndexAsync(IndexKeysDefinition<T> keys, CreateIndexOptions options = null, CreateOneIndexOptions opt = null, CancellationToken ct = default)
        {
            var model = new CreateIndexModel<T>(keys, options);
            return await _collection.Indexes.CreateOneAsync(model, opt, ct);
        }

        public async Task<List<string>> GetIndexesAsync(CancellationToken ct = default)
        {
            using var cursor = await _collection.Indexes.ListAsync(ct);
            var indexes = await cursor.ToListAsync(ct);
            return indexes.Select(i => i["name"].AsString).ToList();
        }
        #endregion

        #region 资源释放
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                if (_pending.Count > 0)
                {
                    try { SaveAsync().Wait(); }
                    catch { /* 忽略 */ }
                }
                _saveTimer?.Dispose();
            }
            _disposed = true;
        }
        #endregion
    }
}
