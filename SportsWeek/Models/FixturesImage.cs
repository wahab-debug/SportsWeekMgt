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
    
    public partial class FixturesImage
    {
        public int id { get; set; }
        public int fixtures_id { get; set; }
        public string image_path { get; set; }
    
        public virtual Fixture Fixture { get; set; }
    }
}
