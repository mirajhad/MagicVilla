﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagicVilla_VillaAPI.Models.Dto
{
    public class LoginResponseDto
    {
        public UserDto User { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
    }
}
