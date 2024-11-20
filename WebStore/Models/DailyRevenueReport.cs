using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebStore.Models
{
    public class DailyRevenueReport
    {
        public string Date { get; set; }
        public decimal TotalAmount { get; set; }
    }
}