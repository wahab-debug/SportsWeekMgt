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
    
    public partial class TeamMember
    {
        public int id { get; set; }
        public string name { get; set; }
        public Nullable<int> reg_number { get; set; }
        public string role { get; set; }
        public Nullable<int> team_id { get; set; }
    
        public virtual Team Team { get; set; }
    }
}
