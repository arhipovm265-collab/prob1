using System;
using System.Collections.Generic;

namespace prob;

public partial class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int StockItemId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual StockItem StockItem { get; set; } = null!;
}
