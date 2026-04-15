using server.Models;

namespace server.Models.DTOs;

public record UserDto(int Id, string Name, string Email, UserRole Role, DateTime CreatedAt);
