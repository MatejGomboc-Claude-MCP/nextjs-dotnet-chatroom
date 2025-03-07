using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ChatRoom.Api.Models.DTOs
{
    public class MessageDto
    {
        [Required]
        public string Id { get; set; } = string.Empty;
        
        [Required]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Message text must be between 1 and 1000 characters")]
        public string Text { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "Username must be between 1 and 50 characters")]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Timestamp { get; set; } = string.Empty;
        
        public bool IsEdited { get; set; } = false;
        
        public string? EditedAt { get; set; } = null;
    }

    public class CreateMessageDto
    {
        [Required(ErrorMessage = "Message text is required")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Message text must be between 1 and 1000 characters")]
        public string Text { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Username is required")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 20 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
        public string Username { get; set; } = string.Empty;
    }
    
    public class UpdateMessageDto
    {
        [Required(ErrorMessage = "Message text is required")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Message text must be between 1 and 1000 characters")]
        public string Text { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Username is required")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 20 characters")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Username can only contain letters, numbers, and underscores")]
        public string Username { get; set; } = string.Empty;
    }
    
    // Custom validator for special characters
    public class NoSpecialCharactersAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            string stringValue = value.ToString();
            
            // Check if the string contains any special characters
            bool containsSpecialChars = !Regex.IsMatch(stringValue, @"^[a-zA-Z0-9\s]+$");
            
            if (containsSpecialChars)
            {
                return new ValidationResult(ErrorMessage ?? "No special characters are allowed");
            }
            
            return ValidationResult.Success;
        }
    }
}