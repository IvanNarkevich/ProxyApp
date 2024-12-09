using Microsoft.AspNetCore.Mvc;
using ProxyApp.Models;
using StackExchange.Redis;
using System.Diagnostics;

namespace ProxyApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly IDatabase _redis;

        private readonly DatabaseContext _db;

        public HomeController(ILogger<HomeController> logger, IConnectionMultiplexer muxer, DatabaseContext db)
        {
            _logger = logger;
            _redis = muxer.GetDatabase();
            _db = db;
        }

        public IActionResult Index()
        {
            Rule rule = _db.Rules.Where(rule => rule.Id == 1).First();
            StatisticViewModel model = new StatisticViewModel(
            ((int)_redis.StringGet("C01_Belarus_WGS84")), rule.allowed_C01_Belarus_WGS84,
            ((int)_redis.StringGet("A06_ATE_TE_WGS84")), rule.allowed_A06_ATE_TE_WGS84,
            ((int)_redis.StringGet("A05_EGRNI_WGS84")), rule.allowed_A05_EGRNI_WGS84,
            ((int)_redis.StringGet("A01_ZIS_WGS84")), rule.allowed_A01_ZIS_WGS84);
            return View(model);
        }
        
        [HttpPost] 
        public IActionResult Index(StatisticViewModel viewModel)
        {
            Rule rule = _db.Rules.Where(rule => rule.Id == 1).First();

            //check if any rule changed
            if (rule.allowed_C01_Belarus_WGS84 != viewModel.Allowed_C01_Belarus_WGS84)
            {
                rule.allowed_C01_Belarus_WGS84 = viewModel.Allowed_C01_Belarus_WGS84;
                _redis.StringSet("C01_Belarus_WGS84", 0);
            }

            if (rule.allowed_A06_ATE_TE_WGS84 != viewModel.Allowed_A06_ATE_TE_WGS84)
            {
                rule.allowed_A06_ATE_TE_WGS84 = viewModel.Allowed_A06_ATE_TE_WGS84;
                _redis.StringSet("A06_ATE_TE_WGS84", 0);
            }

            if (rule.allowed_A05_EGRNI_WGS84 != viewModel.Allowed_A05_EGRNI_WGS84)
            {
                rule.allowed_A05_EGRNI_WGS84 = viewModel.Allowed_A05_EGRNI_WGS84;
                _redis.StringSet("A05_EGRNI_WGS84", 0);
            }

            if (rule.allowed_A01_ZIS_WGS84 != viewModel.Allowed_A01_ZIS_WGS84)
            {
                rule.allowed_A01_ZIS_WGS84 = viewModel.Allowed_A01_ZIS_WGS84;
                _redis.StringSet("A01_ZIS_WGS84", 0);
            }

            StatisticViewModel model = new StatisticViewModel(
            ((int)_redis.StringGet("C01_Belarus_WGS84")), rule.allowed_C01_Belarus_WGS84,
            ((int)_redis.StringGet("A06_ATE_TE_WGS84")), rule.allowed_A06_ATE_TE_WGS84,
            ((int)_redis.StringGet("A05_EGRNI_WGS84")), rule.allowed_A05_EGRNI_WGS84,
            ((int)_redis.StringGet("A01_ZIS_WGS84")), rule.allowed_A01_ZIS_WGS84);

            _db.SaveChanges();
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
