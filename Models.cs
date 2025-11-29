using System.Collections.Generic;

namespace ShopeeServer
{
    public class Order
    {
        public string OrderId { get; set; } = "";
        public int Status { get; set; } = 0; // 0: Mới, 1: Đã xử lý
        public string AssignedTo { get; set; } = "";
        public long CreatedAt { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        public bool Selected { get; set; } = false;
    }

    public class OrderItem
    {
        public long ItemId { get; set; }
        public string ModelName { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public int Quantity { get; set; }
        public string SKU { get; set; } = "";
        public string Location { get; set; } = "";

        // Picking props
        public bool Picked { get; set; } = false;
        public List<string> OrderIds { get; set; } = new List<string>();
        public int TotalQty { get; set; }
        public bool ShowDetail { get; set; } = false;
    }
}