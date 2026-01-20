namespace DAL;

public interface IConfigRepository<TData>
{
    List<(string id, string description)> List();
    Task<List<(string id, string description)>> ListAsync();
    
    string Save(TData data, string? id = null);
    Task<string> SaveAsync(TData data, string? id = null);
    
    TData Load(string id);
    Task<TData> LoadAsync(string id);
    
    void Delete(string id);
    Task DeleteAsync(string id);
}