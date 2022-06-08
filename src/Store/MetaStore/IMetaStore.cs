using System.Data.Common;
using System.Threading.Tasks;
using AppBoxCore;

namespace AppBoxStore;

public interface IMetaStore
{
    Task TryInitStoreAsync();

    Task CreateApplicationAsync(ApplicationModel app, DbTransaction txn);

    Task UpsertFolderAsync(ModelFolder folder, DbTransaction txn);

    Task InsertModelAsync(ModelBase model, DbTransaction txn);

    /// <summary>
    ///  加载单个Model，用于运行时或设计时重新加载
    /// </summary>
    Task<ModelBase> LoadModelAsync(ModelId modelId);
}