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
    
    public partial class Rule
    {
        public int sport_id { get; set; }
        public string rule_of_game { get; set; }
    
        public virtual Sport Sport { get; set; }
    }
}