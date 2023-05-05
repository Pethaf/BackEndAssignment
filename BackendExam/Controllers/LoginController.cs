using BackendExam.DTOS;
using BackendExam.Shared;
using ExamContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Net.Mime;

namespace BackendExam.Controllers
{
    public class LoginController : Controller
    {
        private UserRepository _userRepository;
        private IConfiguration _configuration;
        public LoginController(UserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
        }
        [HttpPost("/login")]
        public async Task<IActionResult> Login([FromBody]UserLoginDTO userLogin)
        {

            if(userLogin.Username == null || userLogin.Password == null)
            {
                return StatusCode(403);
            }
            Task<IActionResult> CheckUser = new(() =>
            {
                var tmp = _userRepository.Users.Find(user => user.UserName == userLogin.Username);
                if (tmp == null)
                {
                    return StatusCode(401);
                }
                if (tmp.Password != userLogin.Password)
                {
                    return StatusCode(403);
                }
                return Accepted(new TransferJWT() { Value = JWTHandler.GenerateToken(userLogin.Username) });
            }); 
            
            CheckUser.Start();
            return await CheckUser;
            
            /*var tmp = _userRepository.Users.Find(user => user.UserName == userLogin.Username); //Move to task 
            if(tmp == null)
            {
                return StatusCode(401);
            }
            if(tmp.Password != userLogin.Password)
            {
                return StatusCode(403);
            }*/

            
            //var tmpString = 
            //    return Accepted("{\"value\" : \"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiJMaW51cyIsIm5iZiI6MTY2NjU0MjM3OCwiZXhwIjoxNjY3MTQ3MTc1LCJpYXQiOjE2NjY1NDIzNzgsImlzcyI6Imh0dHA6Ly9teXNpdGUuY29tIiwiYXVkIjoiaHR0cDovL215YXVkaWVuY2UuY29tIn0.btgf2doOfmIW2xdKcIDIB3fSJPov0FcODoBe4Ojo570\"}");
            //var tmp2 = Accepted(tmpString);
            //return tmp2;
            //return Accepted($"{{ \"Value\" : eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiJMaW51cyIsIm5iZiI6MTY2NjU0MjM3OCwiZXhwIjoxNjY3MTQ3MTc1LCJpYXQiOjE2NjY1NDIzNzgsImlzcyI6Imh0dHA6Ly9teXNpdGUuY29tIiwiYXVkIjoiaHR0cDovL215YXVkaWVuY2UuY29tIn0.btgf2doOfmIW2xdKcIDIB3fSJPov0FcODoBe4Ojo570 }}");
            //HttpResponseMessage resp = new HttpResponseMessage(HttpStatusCode.Accepted);
            //resp.Content = new StringContent($"value: {GenerateJWT.GetToken(_configuration.GetSection("Jwt").GetSection("Key").Value, tmp.UserName)}");
            //return (IActionResult)resp; 
            //return Request.CreateResponse(
            //    HttpStatusCode.Accepted,
            //    GenerateJWT.GetToken(_configuration.GetSection("Jwt").GetSection("Key").Value, tmp.UserName));
            //return Accepted(GenerateJWT.GetToken(_configuration.GetSection("Jwt").GetSection("Key").Value,tmp.UserName)); 
        }
    }
}
