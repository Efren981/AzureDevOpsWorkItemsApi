using System.ComponentModel.DataAnnotations;

namespace AzureDevOpsWorkItemsApi.Models
{
    public class LoginRequest
    {
        [Required]
        public string Username { get; set; }
        
        [Required]
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Message { get; set; }
        public DateTime Expiration { get; set; }
    }
}