using BackendExam.DTOS;
using BackendExam.Shared;
using ExamContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Collections.Immutable;
using System.Net;

namespace BackendExam.Controllers
{
    public class ResturantController : Controller
    {
        private UserRepository _userRepository;
        private TimeClock _timeClock;
        private Warehouse _warehouse;

        public ResturantController(UserRepository uR, TimeClock tc, Warehouse warehouse)
        {
            _userRepository = uR;
            _timeClock = tc;
            _warehouse = warehouse;
        }
        [HttpPost("/restaurant/add-ingredients")]
        public async Task<IActionResult> AddIngredient([FromBody] AddIngredient addIngredient)
        {
            var tmp = Request.Headers["Authorization"];
            var token = tmp.ToString().Substring(7);
            if (!JWTHandler.ValidateCurrentToken(token))
            {
                return StatusCode(403);
            }
            var decodedToken = JWTHandler.DecodeToken(token);
            var name = decodedToken.Claims.Where(claim => claim.Type == "nameid").FirstOrDefault().Value;
            var user = _userRepository.Users.Find(user => user.UserName == name);
            if (!user.Roles.Any(role => role.Name == "Manager"))
            {
                return StatusCode(403);
            }
            if((int) addIngredient.ingredientType == 0)
            {
                return BadRequest();
            }
            lock (_warehouse)
            {
                _warehouse.AddIngredient(addIngredient.ingredientType, addIngredient.Id);
            }
            return Accepted();
        }
        [HttpPost("/restaurant/enter")]
        public async Task<IActionResult> PunchIn()
        {
                var tmp = Request.Headers["Authorization"];
                var token = tmp.ToString().Substring(7);
                if (!JWTHandler.ValidateCurrentToken(token))
                {
                    return StatusCode(403);
                }
                var decodedToken = JWTHandler.DecodeToken(token);
                var name = decodedToken.Claims.Where(claim => claim.Type == "nameid").FirstOrDefault().Value;
                var user = _userRepository.Users.Find(user => user.UserName == name);
                if (user.Roles.Any(role => role.Name == "Manager"))
                {
                    return StatusCode(403);
                }
                    try
                    {
                      lock (_timeClock)
                       {
                        if(_timeClock.GetUser(user) != null)
                        {
                            return BadRequest();
                        }
                        _timeClock.Enter(user);
                       }
                    }
                    catch(ExamException)
                    {
                        return StatusCode(500); 
                    }
                return Accepted();
        }
            
        [HttpPost("/restaurant/leave")]
        public async Task<IActionResult> PunchOut()
        {
            Task<IActionResult> PunchingOut = new(() =>
            {
                var tmp = Request.Headers["Authorization"];
                var token = tmp.ToString().Substring(7);
                if (!JWTHandler.ValidateCurrentToken(token))
                {
                    return Forbid();
                }
                var decodedToken = JWTHandler.DecodeToken(token);
                var name = decodedToken.Claims.Where(claim => claim.Type == "nameid").FirstOrDefault().Value;
                var user = _userRepository.Users.Find(user => user.UserName == name);
                lock (_timeClock)
                {
                    if(_timeClock.GetUser(user) == null)
                    {
                        return BadRequest();
                    }
                    _timeClock.Leave(user);
                }
                return Accepted();
            });
            PunchingOut.Start();
            return await PunchingOut;
            
        }
    }
}
