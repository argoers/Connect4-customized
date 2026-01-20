namespace BLL;

public class BaseEntity
{
    public Guid Id { get; set; } =  Guid.NewGuid();
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}