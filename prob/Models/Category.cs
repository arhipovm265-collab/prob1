using System;
using System.Collections.Generic;

namespace prob;

public partial class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Subcategory> Subcategories { get; set; } = new List<Subcategory>();
}
