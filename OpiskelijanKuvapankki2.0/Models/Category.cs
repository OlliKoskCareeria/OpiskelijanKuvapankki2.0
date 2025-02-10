using System;
using System.Collections.Generic;

namespace OpiskelijanKuvapankki2_0.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public virtual ICollection<Image> Images { get; set; } = new List<Image>();
}
