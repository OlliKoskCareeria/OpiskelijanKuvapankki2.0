using System;
using System.Collections.Generic;

namespace OpiskelijanKuvapankki2_0.Models;

public partial class Login
{
    public int LoginId { get; set; }

    public string Email { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Contact { get; set; }

    public string Pword { get; set; } = null!;
}
