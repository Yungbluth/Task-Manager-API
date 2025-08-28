using System.ComponentModel.DataAnnotations;

namespace TaskApi;

public class User
{
    public int Id { get; set; }
    [Required, MinLength(3)] public string Username { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
    public List<Todo> Todos { get; set; } = new();
}

public class Todo
{
    public int Id { get; set; }
    [Required] public string Title { get; set; } = "";
    public bool Done { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
}

// DTOs
public record RegisterDto(string Username, string Password);
public record LoginDto(string Username, string Password);
public record TodoCreateDto(string Title, bool Done);
public record TodoUpdateDto(string Title, bool Done);