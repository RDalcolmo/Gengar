using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Birthday_Bot.Models
{
    public partial class Tblbirthdays
    {
        [Key]
        public long Userid { get; set; }
        public DateTime? Birthday { get; set; }
        public string Comments { get; set; }
    }
}
