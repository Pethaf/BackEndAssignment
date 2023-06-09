﻿using BackendExam.DTOS;
using ExamContext;
using ExamContext.Chef;

namespace BackendExam
{
    public class Chef : IChef
    {
        private OrderQueue _orderQueue;
        private Warehouse _warehouse;
        private Cookbook _cookbook;
        private Oven _oven;
        private int _id;
        private DeliveryDesk _deliveryDesk;

        public string Name => $"SwedishChef{_id}";
        public Chef(OrderQueue orderqueue, Warehouse warehouse, Cookbook cookbook, Oven oven, int Id, DeliveryDesk deliveryDesk)
        {
            _orderQueue = orderqueue;
            _warehouse = warehouse;
            _cookbook = cookbook;
            _oven = oven;
            _id = Id;
            _deliveryDesk = deliveryDesk;
        }
        public void Run()
        {
            Thread.Sleep(100);
            while (true)
            {
                if (_orderQueue.Queue.Count != 0)
                {
                    List<Task<Pizza>> pizzasInOven = new();
                    Order? theOrder = null;
                    bool hasOrder = false;
                    lock (_orderQueue)
                    {
                         hasOrder = _orderQueue.Queue.TryDequeue(out theOrder);

                    }
                    if (hasOrder)
                    {
                        foreach (var aPizza in theOrder.Pizzas) // Do this for every pizza in the order 
                        {
                            int numberOfPizzas = aPizza.quantity;
                            int counter = 0;
                            var ingredientList = _cookbook.CookbookList[aPizza.pizzaType]; //Lookup what Ingredients are needed for the pizza                    

                            while (counter != numberOfPizzas)
                            {

                                List<Ingredient> ingredients = new();
                                foreach (var ingredientAndQuantity in ingredientList)  // For each ingredient from the cookbook 
                                {

                                    for (int i = 0; i != ingredientAndQuantity.quantity; i++) //Try to get the needed number of ingredients from the warehouse 
                                    {
                                        Ingredient? theIngredient;
                                        lock (_warehouse)
                                        {
                                            theIngredient = _warehouse.GetIngredient(ingredientAndQuantity.ingredient, this);
                                        }
                                            if (theIngredient != null)
                                            {
                                                ingredients.Add(theIngredient);
                                            }
                                        
                                    }

                                }
                                pizzasInOven.Add(_oven.Bake(ingredients));
                                counter++;
                            }

                        }
                        var finishedOrder = Task.WhenAll(pizzasInOven).Result;
                        lock (_deliveryDesk)
                        {
                            _deliveryDesk.FinishedOrders.Add(theOrder.Id, finishedOrder.ToList());
                        }
                    }
                }

                //1.Dictionary to store pizzatype and number of pizzas
                //2.Lookup ingredients needed for pizza in cookbook.
                //3.Lock warehouse while trying to get ingredients for pizza
                //4.Realease lock

            }
        }
    }
}