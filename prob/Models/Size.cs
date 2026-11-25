using System;
using System.Collections.Generic;

namespace prob;

public partial class Size
{
    public int Id { get; set; }

    public decimal Value { get; set; }

    public virtual ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
}
