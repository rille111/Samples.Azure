using System;
using System.Collections.Generic;

namespace Orders.Shared
{
    public interface IOrderRepository
    {
        OrderModel Get(Guid identifier);
        List<OrderModel> GetAll();
        void Add(OrderModel entity);
    }
}