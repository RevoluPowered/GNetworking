﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GNetworking.Data
{
    public class Message
    {
        public User User { get; set; }
        public ChatChannel Channel { get; set; }
        public string Text { get; set; }
    }
}
