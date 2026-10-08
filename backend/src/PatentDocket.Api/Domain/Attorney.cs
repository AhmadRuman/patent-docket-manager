namespace PatentDocket.Api.Domain;

/// <summary>A docketing user: an attorney, agent or paralegal responsible for matters.</summary>
public class Attorney
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Initials { get; set; }
    public required string Email { get; set; }
    public string Role { get; set; } = "Attorney";

    public List<Matter> Matters { get; set; } = [];
}
