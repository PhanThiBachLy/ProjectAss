using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace WebStore.Models
{
    public partial class OrderDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderDetailId { get; set; }
        public int? ProductId { get; set; }
        public int? OrderId { get; set; }
        public string ProductType { get; set; }
        public decimal UnitPrice { get; set; }
        public int? Quantity { get; set; }
        public decimal? TotalAmount { get; set; }
        public int? VoucherId { get; set; }

        public virtual OrderItem Order { get; set; }
        public virtual Product Product { get; set; }
        public virtual Voucher Voucher { get; set; }
    }
}
