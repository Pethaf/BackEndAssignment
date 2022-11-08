using ExamContext;
using ExamContext.Chef;

namespace BackendExam
{
    public class ChefFactory : IChefFactory
    {
        private int chefCount = 0;
        private OrderQueue _orderQueue; 
        private Warehouse _warehouse;
        private Cookbook _cookbook;
        private Oven _oven;
        private DeliveryDesk _deliveryDesk;

        public ChefFactory(OrderQueue orderQueue, Warehouse warehouse,Cookbook cookbook, Oven oven, DeliveryDesk deliveryDesk)
        {
            _orderQueue = orderQueue;
            _warehouse = warehouse;
            _cookbook = cookbook;
            _oven = oven;
            _deliveryDesk = deliveryDesk;
        }
        public IChef CreateChef()
        {
            chefCount++;
            return new Chef(this._orderQueue, this._warehouse, this._cookbook, this._oven,chefCount, _deliveryDesk);
        }
    }
}
