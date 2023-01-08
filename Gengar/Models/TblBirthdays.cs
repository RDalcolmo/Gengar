using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gengar.Models
{
    public partial class Tblbirthdays
    {
        [Key]
        public ulong Userid { get; set; }
        public DateTime Birthday { get; set; }
        public string? Comments { get; set; }

        public int? DayOfYear { get; set; }
    }
}
