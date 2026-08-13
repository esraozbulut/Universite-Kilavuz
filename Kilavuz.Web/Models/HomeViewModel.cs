using System.Collections.Generic;
using Kilavuz.Web.Domain.Entities;

namespace Kilavuz.Web.Models;

public class HomeViewModel
{
    public List<Domain.Entities.Application> Applications { get; set; } = new();
}
