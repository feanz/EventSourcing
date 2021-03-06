﻿using System.ComponentModel.DataAnnotations;

namespace Eventus.Samples.Web.Features.Account
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
