using System.Linq;
using System.Web.Mvc;
using WebStore.Models;

public class StatisticsController : Controller
{
    private StoreDbContext db = new StoreDbContext();

    [HttpPost]
    public ActionResult GetDailyRevenue(int month, int year)
    {
        var dailyRevenueData = db.OrderItem
            .Where(oi => oi.CreatedDate.HasValue && oi.CreatedDate.Value.Month == month && oi.CreatedDate.Value.Year == year)
            .GroupBy(oi => oi.CreatedDate.Value.Day)
            .Select(g => new DailyRevenueReport
            {
                Date = g.Key.ToString(),
                TotalAmount = g.Sum(oi => oi.GrandTotal ?? 0)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return Json(dailyRevenueData);
    }
}