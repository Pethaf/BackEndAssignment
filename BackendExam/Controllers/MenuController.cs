using BackendExam.DTOS;
using ExamContext;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace BackendExam.Controllers
{
    public class MenuController : Controller
    {
        private Menu _menu;
        public MenuController(Menu menu)
        {
            _menu = menu;
        }
        [HttpGet("/menu")]
        public async Task<List<PizzaRepresentation>> GetMenu()
        {
            Task<List<PizzaRepresentation>> getList = new(() =>
            {
                List<PizzaRepresentation> pizzas = new();
                foreach(MenuItem aPIzza in _menu.MenuItems)
                {
                    pizzas.Add(new PizzaRepresentation { name = aPIzza.Name.Name, price = aPIzza.Price });
                };
                return pizzas;
            });
            getList.Start();
            return await getList; 
            //List<PizzaRepresentantion> pizzas = new(); 
            //foreach(MenuItem menuItem in _menu.MenuItems)
            //{
            //    pizzas.Add(new PizzaRepresentantion { name = menuItem.Name.Name, price = menuItem.Price });
            //}
            //return pizzas;
            
        }
    }
}
