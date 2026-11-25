using System;
using System.Collections.Generic;

namespace prob;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int ManufacturerId { get; set; }

    public int SubcategoryId { get; set; }

    public string? ImageFileName { get; set; }

    public string? Description { get; set; }

    public string? Composition { get; set; }

    public decimal Price { get; set; }

    public virtual Manufacturer Manufacturer { get; set; } = null!;

    public virtual ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();

    public virtual Subcategory Subcategory { get; set; } = null!;
}
