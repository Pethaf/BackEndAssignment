using BackendExam.DTOS;
using ExamContext;
using Microsoft.AspNetCore.Mvc;

namespace BackendExam.Controllers
{
    public class RegisterController : Controller
    {
        private UserRepository _userRepository;
        public RegisterController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }
        [HttpPost("/register")]
        public async Task<IActionResult> RegisterUser([FromBody]RegisterUserDTO registerUser)
        {
            if(_userRepository.Users.Find(existingUser => existingUser.UserName == registerUser.Username) != null)
            {
                return StatusCode(409);
            }
            try
            {
                Guid g = Guid.NewGuid();
                string guidAsString = g.ToString();
                List <Role> roles = new List<Role>(); 
                foreach(string RoleToRegister in registerUser.Roles)
                {
                    roles.Add(new Role(RoleToRegister));
                }
                _userRepository.Users.Add(
                    new User(guidAsString,
                             registerUser.Username,
                             registerUser.Password,
                             roles));
                return Ok();
            }
            catch(Exception err)
            {
                return StatusCode(500);
            }
            
        }
    }
}
