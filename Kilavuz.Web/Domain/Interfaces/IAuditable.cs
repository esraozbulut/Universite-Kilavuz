using System;

namespace Kilavuz.Web.Domain.Interfaces;

public interface IAuditable
{
    int CreatedByUserId { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}
