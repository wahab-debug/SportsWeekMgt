using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SportsWeek.DTOs
{
    public class TeamDTOs
    {
        public string Tname { get; set; }
        public string className { get; set; }
        public int captain_id { get; set; }
        public int sport_id { get; set; }
        public string Image_path { get; set; }
        public byte teamStatus { get; set; }
        public string TeamType { get; set; }
    }
}