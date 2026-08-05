using Kilavuz.Web.Domain.Interfaces;

namespace Kilavuz.Web.Domain.Entities;

public class Role : IEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
