using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.Helpers;
using API.Models;
using API.Dtos;
using API.Helper.Factory;
namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IJwtFactory _jwtFactory;
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly API.Helper.Factory.JwtIssuerOptions _jwtOptions;
        private readonly DPContext _context;
        private readonly IMapper _mapper;
        public AuthController(UserManager<AppUser> userManager, IMapper mapper, DPContext context, IJwtFactory jwtFactory, IOptions<API.Helper.Factory.JwtIssuerOptions> jwtOptions)
        {
            _userManager = userManager;
            _jwtFactory = jwtFactory;
            _jwtOptions = jwtOptions.Value;
            _mapper = mapper;
            _context = context;
            _serializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
        }
        static string id;
        [HttpPost("registerCustomer")]
        public async Task<IActionResult> Post([FromBody] JObject json)
        {
            var model = JsonConvert.DeserializeObject<RegistrationViewModel>(json.GetValue("data").ToString());
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            model.Quyen = "Customer";
            var userIdentity = _mapper.Map<AppUser>(model);
            var result = await _userManager.CreateAsync(userIdentity, model.Password);
            AppUser user = new AppUser();
            user = await _context.AppUsers.FirstOrDefaultAsync(s => s.Id == userIdentity.Id);
            _context.AppUsers.Update(user);
            if (!result.Succeeded) return new BadRequestObjectResult(Errors.AddErrorsToModelState(result, ModelState));
            await _context.JobSeekers.AddAsync(new JobSeeker { Id_Identity = userIdentity.Id, Location = model.Location });
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpPost("getDiaChi")]
        public async Task<IActionResult> GetDiaChi([FromBody] JObject json)
        {
            var id = json.GetValue("id_user").ToString();
            var resuft = await _context.AppUsers.Where(d => d.Id == id).Select(d => d.DiaChi).SingleOrDefaultAsync();
            return Json(resuft);
        }
        // POST api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Post([FromBody] CredentialsViewModel credentials)
        {
            try
            {
                // Ghi log thông tin đăng nhập để debug
                Console.WriteLine($"Login attempt for username: {credentials?.UserName}");
                
                // Kiểm tra credentials
                if (credentials == null)
                {
                    Console.WriteLine("Credentials object is null");
                    return BadRequest(Errors.AddErrorToModelState("login_failure", "Thông tin đăng nhập không hợp lệ", ModelState));
                }

                if (!ModelState.IsValid)
                {
                    Console.WriteLine("ModelState is invalid");
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(x => x.Errors)
                        .Select(x => x.ErrorMessage));
                    Console.WriteLine($"ModelState errors: {errors}");
                    return BadRequest(ModelState);
                }

                // Lấy identity
                var identity = await GetClaimsIdentity(credentials.UserName, credentials.Password);
                if (identity == null)
                {
                    Console.WriteLine("Identity is null after GetClaimsIdentity");
                    return BadRequest(Errors.AddErrorToModelState("login_failure", "Tên đăng nhập hoặc mật khẩu không đúng.", ModelState));
                }

                // Lấy thông tin ID từ claims
                var idClaim = identity.Claims.FirstOrDefault(c => c.Type == "id");
                if (idClaim == null)
                {
                    Console.WriteLine("ID claim not found in identity");
                    return BadRequest(Errors.AddErrorToModelState("login_failure", "Không tìm thấy thông tin người dùng.", ModelState));
                }

                var userId = idClaim.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("User ID is null or empty");
                    return BadRequest(Errors.AddErrorToModelState("login_failure", "Không tìm thấy thông tin người dùng.", ModelState));
                }

                // Cập nhật biến id tĩnh
                id = userId;
                Console.WriteLine($"Found user with ID: {id}");

                // Tìm người dùng trong cơ sở dữ liệu
                var user = await _context.AppUsers.FirstOrDefaultAsync(s => s.Id == userId);
                if (user == null)
                {
                    Console.WriteLine("User not found in database");
                    return BadRequest(Errors.AddErrorToModelState("login_failure", "Không tìm thấy thông tin người dùng.", ModelState));
                }

                // Tạo token và trả về response
                var token = await _jwtFactory.GenerateEncodedToken(credentials.UserName, identity);
                Console.WriteLine("JWT token generated successfully");

                var response = new
                {
                    id = userId,
                    quyen = user.Quyen,
                    hinh = user.ImagePath,
                    fullname = $"{user.FirstName} {user.LastName}",
                    email = user.Email,
                    auth_token = token,
                    expires_in = (int)_jwtOptions.ValidFor.TotalSeconds
                };

                var json = JsonConvert.SerializeObject(response, _serializerSettings);
                return new OkObjectResult(json);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi chi tiết
                Console.WriteLine($"Lỗi đăng nhập: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }
                
                // Trả về lỗi chi tiết để debugging (chỉ nên dùng trong môi trường dev)
                return StatusCode(500, new { error = "Đã xảy ra lỗi khi đăng nhập", details = ex.Message, stackTrace = ex.StackTrace });
            }
        }
        [HttpPost("logout")]
        public IActionResult logout()
        {
            id = null;
            return Ok();
        }
        public async Task<IActionResult> UpdateUser([FromBody] CredentialsViewModel credentials)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var identity = await GetClaimsIdentity(credentials.UserName, credentials.Password);
            if (identity == null)
            {
                return BadRequest(Errors.AddErrorToModelState("login_failure", "Invalid username or password.", ModelState));
            }
            var response = new
            {
                id = identity.Claims.Single(c => c.Type == "id").Value,
                quyen = _context.AppUsers.FirstOrDefault(s => s.Id == id).Quyen,
                email = _context.AppUsers.FirstOrDefault(s => s.Id == id).Email,
                auth_token = await _jwtFactory.GenerateEncodedToken(credentials.UserName, identity),
                expires_in = (int)_jwtOptions.ValidFor.TotalSeconds
            };
            var json = JsonConvert.SerializeObject(response, _serializerSettings);
            return new OkObjectResult(json);
        }
        private async Task<ClaimsIdentity> GetClaimsIdentity(string userName, string password)
        {
            try
            {
                Console.WriteLine($"GetClaimsIdentity called for username: {userName}");
                
                if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
                {
                    Console.WriteLine("Username or password is null or empty");
                    return await Task.FromResult<ClaimsIdentity>(null);
                }

                // get the user to verify
                var userToVerify = await _userManager.FindByNameAsync(userName);
                if (userToVerify == null)
                {
                    Console.WriteLine($"User not found with username: {userName}");
                    return await Task.FromResult<ClaimsIdentity>(null);
                }

                Console.WriteLine($"Found user with ID: {userToVerify.Id}");

                // check the credentials
                if (await _userManager.CheckPasswordAsync(userToVerify, password))
                {
                    Console.WriteLine("Password check passed");
                    try
                    {
                        AuthHistory auth = new AuthHistory();
                        auth.IdentityId = userToVerify.Id;
                        auth.Datetime = DateTime.Now;
                        _context.AuthHistories.Add(auth);
                        await _context.SaveChangesAsync();
                        Console.WriteLine("Auth history saved");
                        
                        id = userToVerify.Id;
                        var claimsIdentity = _jwtFactory.GenerateClaimsIdentity(userName, userToVerify.Id);
                        Console.WriteLine("Claims identity generated successfully");
                        return claimsIdentity;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in GetClaimsIdentity after password check: {ex.Message}");
                        throw;
                    }
                }

                Console.WriteLine("Password check failed");
                return await Task.FromResult<ClaimsIdentity>(null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetClaimsIdentity: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
        [HttpGet("AuthHistory")]
        public async Task<ActionResult<AppUser>> GetAuthHistory()
        {
            AppUser appUser = new AppUser();
            appUser = await _context.AppUsers.FindAsync(id);
            return appUser;
        }
    }
}