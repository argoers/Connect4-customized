namespace DAL;

public interface IGameRepository<TData>
{
    List<(string id, string description, string date, bool isFinished)> List();
    Task<List<(string id, string description, string date, bool isFinished)>> ListAsync();
    
    string Save(TData data);
    Task<string> SaveAsync(TData data);
    
    TData Load(string id);
    Task<TData> LoadAsync(string id);
    
    void Delete(string id);
    Task DeleteAsync(string id);
}