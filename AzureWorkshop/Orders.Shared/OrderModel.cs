using System;

namespace Orders.Shared
{
    public class OrderModel
    {
        public Guid Identifier { get; set; }
        public string Name { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}