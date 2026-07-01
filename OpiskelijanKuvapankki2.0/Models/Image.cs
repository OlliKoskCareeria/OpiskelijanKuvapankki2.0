using System;
using System.Collections.Generic;

namespace OpiskelijanKuvapankki2_0.Models;

public partial class Image
{
    public int ImageId { get; set; }

    public int? LoginId { get; set; }

    public int? CategoryId { get; set; }

    public required string ImageName { get; set; }

    public string? ImageLink { get; set; }

    public byte[]? ImageBytes { get; set; }

    public virtual Category? Category { get; set; }
}
