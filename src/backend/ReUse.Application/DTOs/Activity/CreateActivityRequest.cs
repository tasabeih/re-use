using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReUse.Application.DTOs.Activity;

public record CreateActivityRequest(
    Guid UserId,
    Guid? ProductId,
    string Type,
    string? Description,
    string? Metadata
);