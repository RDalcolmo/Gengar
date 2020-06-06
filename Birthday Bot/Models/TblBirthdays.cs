using System;
using System.Collections.Generic;

namespace Birthday_Bot.Models
{
    public partial class Tblbirthdays
    {
        public long Userid { get; set; }
        public DateTime? Birthday { get; set; }
        public string Comments { get; set; }
    }
}
