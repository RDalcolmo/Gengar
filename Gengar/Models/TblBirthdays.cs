using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Gengar.Models
{
    public partial class Tblbirthdays
    {
        [Key]
        public long Userid { get; set; }
        public DateTime? Birthday { get; set; }
        public string Comments { get; set; }
    }
}
