namespace Domain.Entities;

public class Document
{
    public int ID { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}