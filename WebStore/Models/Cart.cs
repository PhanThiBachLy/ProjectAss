using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace WebStore.Models
{
    public partial class Cart
    {
        public Cart()
        {
            CartDetail = new HashSet<CartDetail>();
        }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartId { get; set; }
        public int? UserId { get; set; }
        public decimal? TotalPayment { get; set; }
        public int? SelectedProductCount { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<CartDetail> CartDetail { get; set; }
    }
}
