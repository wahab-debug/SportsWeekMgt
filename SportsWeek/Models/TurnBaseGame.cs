//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SportsWeek.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class TurnBaseGame
    {
        public int id { get; set; }
        public int fixture_id { get; set; }
        public Nullable<int> team_id { get; set; }
        public Nullable<int> player_id { get; set; }
        public Nullable<int> rating_adjustment { get; set; }
    
        public virtual Fixture Fixture { get; set; }
        public virtual Player Player { get; set; }
        public virtual Team Team { get; set; }
    }
}
