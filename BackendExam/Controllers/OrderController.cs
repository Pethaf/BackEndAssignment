using BackendExam.DTOS;
using BackendExam.Shared;
using ExamContext;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mime;

namespace BackendExam.Controllers
{
    public class OrderController : Controller
    {
        private OrderQueue _orderQueue;
        private Menu _menu;
        private DeliveryDesk _deliveryDesk;
        public OrderController(OrderQueue orderQueue, Menu menu,DeliveryDesk deliveryDesk)
        {
            _orderQueue = orderQueue;
            _menu = menu;  
            _deliveryDesk = deliveryDesk;
        }
        [HttpGet("/order/{orderId}")]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            lock (_deliveryDesk)
            {
                if (_deliveryDesk.FinishedOrders.ContainsKey(orderId))
                {
                    List<Pizza> delivery;
                    delivery = _deliveryDesk.FinishedOrders[orderId];
                    _deliveryDesk.FinishedOrders.Remove(orderId);
                    var OrderToDeliver = new PizzaOrderDoneDTO();
                    OrderToDeliver.status = "done";
                    foreach (Pizza thePizza in delivery)
                    {
                        OrderToDeliver.order.Add(new DeliveredPizza() { id = thePizza.Id, type = thePizza.Name.Name });
                    }
                    return Ok(OrderToDeliver);
                }
            }
            return NotFound();
        }
        [HttpPost("/order")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PlaceOrder([FromBody]List<string> pizzaOrder)
        {
            if(pizzaOrder.Count == 0)
            {
                return BadRequest();
            }
            Task<bool> checkNames = new(() =>
            {
                bool allNamesValid = true;
                foreach(string orderedPizzaName in pizzaOrder)
                {
                    if(!_menu.MenuItems.Any(aMenuItem => aMenuItem.Name.Name == orderedPizzaName))                    
                    {
                        allNamesValid = false;
                        break; 
                    }
                }
                return allNamesValid;
            });
            checkNames.Start();
            if(!await checkNames)
            {
                return BadRequest();
            }

            Dictionary<String, int> quantityOrder = new(); 
            foreach(string pizzaName in pizzaOrder)
            {
                if (quantityOrder.ContainsKey(pizzaName))
                {
                    quantityOrder[pizzaName]++;
                }
                else
                {
                    quantityOrder[pizzaName] = 1;
                }
            }
            
            List < (PizzaType, int) > theOrder= new();
            foreach(var Pizza in quantityOrder)
            {
                theOrder.Add((new PizzaType(Pizza.Key), Pizza.Value));
            }
            Order handOffOrder = new Order(theOrder);
            lock (_orderQueue)
            {
                _orderQueue.Queue.Enqueue(handOffOrder);
            }
                return Created($"/order/{handOffOrder.Id}",null);
         
        }

    }
}
