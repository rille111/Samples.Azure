using System;
using System.Collections.Generic;
using System.Web.Http;
using Orders.AzDocDbRepository;
using Orders.AzStorageRepository;
using Orders.Shared;
using Orders.SqlRepository;

namespace Orders.WebApi.Controllers
{
    public class OrderController : ApiController
    {
        private readonly IOrderRepository _repo;

        public OrderController()
        {
            //_repo = new OrderMockRepository();
            //_repo = new OrderSqlRepository();
            //_repo = new OrderAzTableStorageRepository();
            _repo = new OrderAzDocDbRepository();
        }

        [HttpGet]
        [Route("orders")]
        public IEnumerable<OrderModel> Get()
        {
            return _repo.GetAll();
        }

        [HttpGet]
        [Route("orders/{id:guid}")]
        public OrderModel Get(Guid id)
        {
            return _repo.Get(id);
        }

        [HttpPost]
        [Route("orders")]
        public void Post([FromBody]OrderModel order)
        {
            _repo.Add(order);
        }
    }
}