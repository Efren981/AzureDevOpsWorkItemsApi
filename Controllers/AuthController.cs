using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AzureDevOpsWorkItemsApi.Models;

namespace AzureDevOpsWorkItemsApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint para generar un token JWT
        /// </summary>
        [HttpPost("token")]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
        public IActionResult GetToken([FromBody] LoginRequest request)
        {
            // En un entorno real, validarías las credenciales contra una base de datos
            // Para este ejemplo, simulamos una validación
            if (!IsValidUser(request.Username, request.Password))
            {
                _logger.LogWarning("Intento de inicio de sesión fallido para el usuario: {Username}", request.Username);
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = "Credenciales inválidas"
                });
            }

            var token = GenerateJwtToken(request.Username);
            var expiration = DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:ExpiryInMinutes"]));

            _logger.LogInformation("Token JWT generado exitosamente para el usuario: {Username}", request.Username);

            return Ok(new LoginResponse
            {
                Success = true,
                Token = token,
                Expiration = expiration,
                Message = "Autenticación exitosa"
            });
        }

        // En un entorno real, este método validaría contra una base de datos
        private bool IsValidUser(string username, string password)
        {
            // IMPORTANTE: Esta es una validación de ejemplo
            // En producción, debes implementar una autenticación robusta
            return !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);
        }

        private string GenerateJwtToken(string username)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] 
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(JwtRegisteredClaimNames.Sub, username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                }),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryInMinutes"])),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}