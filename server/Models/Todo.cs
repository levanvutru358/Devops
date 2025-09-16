namespace EmoApi.Models;

public record Todo(long Id, string Title, bool IsDone, DateTime CreatedAt);
public record TodoCreate(string Title, bool IsDone);

