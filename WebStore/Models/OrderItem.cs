using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace WebStore.Models
{
    public partial class OrderItem
    {
        public OrderItem()
        {
            OrderDetail = new HashSet<OrderDetail>();
        }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }
        public int? UserId { get; set; }
        public string MessageForSeller { get; set; }
        public string ShippingAddress { get; set; }
        public int? VoucherId { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? ShippingFee { get; set; }
        public decimal? GrandTotal { get; set; }
        public DateTime? CreatedDate { get; set; }

        public virtual User User { get; set; }
        public virtual Voucher Voucher { get; set; }
        public virtual ICollection<OrderDetail> OrderDetail { get; set; }
    }
}
