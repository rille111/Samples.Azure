using System;
using System.Collections.Generic;
using System.Linq;

namespace Orders.Shared
{
    public class OrderMockRepository : IOrderRepository
    {
        private readonly List<OrderModel> _orders = new List<OrderModel>();

        public OrderMockRepository()
        {
            _orders.Add(new OrderModel() { Identifier = Guid.Parse("d3064035-e745-41cf-a121-168e1c79782b"), Name = "Foo"});
            _orders.Add(new OrderModel() { Identifier = Guid.Parse("6dd256e9-d85c-47d0-9d7b-33095dffa63c"), Name = "Bar" });
            _orders.Add(new OrderModel() { Identifier = Guid.Parse("8dd372bb-bf96-4ca9-973a-888f203b7e5d"), Name = "OrderThree" });
        }

        public OrderModel Get(Guid identifier)
        {
            return _orders.First(p => p.Identifier == identifier);
        }

        public List<OrderModel> GetAll()
        {
            return _orders.ToList();
        }

        public void Add(OrderModel entity)
        {
            _orders.Add(entity);
        }
    }
}