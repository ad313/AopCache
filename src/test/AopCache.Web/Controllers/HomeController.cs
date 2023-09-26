using AopCache.Core.Abstractions;
using AopCache.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace AopCache.Web.Controllers
{
    public class HomeController : Controller
    {
        private ITestService TestService { get; set; }

        private TestSingleClass TestSingleClass { get; set; }

        private IAopCacheProvider AopCacheProvider { get; set; }

        public HomeController(ITestService testService, TestSingleClass testSingleClass, IAopCacheProvider aopCacheProvider)
        {
            TestService = testService;

            TestSingleClass = testSingleClass;

            AopCacheProvider = aopCacheProvider;
        }

        /// <summary>
        /// 点击首页，清除某个key
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            //在这里清除某个key
            //清除 GetUserInfo
            //AopCacheProvider.Remove("ccc_1000_1");


            //var model = new UserInfo()
            //{
            //    Id = 100,
            //    Name = "aa",
            //    UserInfo2 = new UserInfo2()
            //    {
            //        Id = 111,
            //        Name = "bb"
            //    }
            //};

            //var list = new List<UserInfo>();
            //for (int i = 0; i < 100; i++)
            //{
            //    list.Add(new UserInfo()
            //    {
            //        Id = 1,
            //        Name = "hahhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh",
            //        UserInfo2 = new UserInfo2()
            //        {
            //            Id = 2,
            //            Name = "沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发沙发"
            //        }
            //    });
            //}

            //var list = new List<string>();
            //for (int i = 0; i < 10000; i++)
            //{
            //    list.Add("hahhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh");
            //}

            //Task.Run(async () =>
            //{
            //    var watch = Stopwatch.StartNew();

            //    await EventBusProvider.PublishQueueAsync("abc", list);
            //    await EventBusProvider.PublishQueueAsync("abc", list);
            //    await EventBusProvider.PublishQueueAsync("abc", list);

            //    Console.WriteLine($"---{watch.ElapsedMilliseconds}");
            //});

            //EventBusProvider.PublishQueueAsync("abc", new List<string>() { "1", });

            //for (int i = 0; i < 100; i++)
            //{
            //    EventBusProvider.PublishQueueAsync("abc", new List<string>() { "1", });
            //    EventBusProvider.PublishQueueAsync("abc", new List<string>() { "2", "3" });
            //    EventBusProvider.PublishQueueAsync("abc", new List<string>() { "4", "5", "6" });
            //    EventBusProvider.PublishQueueAsync("abc", new List<string>() { "7", "8", "9", "10" });
            //    EventBusProvider.PublishQueueAsync("abc", new List<string>() { "11", "12", "13", "14", "15" });
            //}

            //EventBusProvider.PublishQueueAsync("abc", new List<string>() { "1", });
            //EventBusProvider.PublishQueueAsync("abc", new List<string>() { "2", "3" });
            //EventBusProvider.PublishQueueAsync("abc", new List<string>() { "4", "5", "6" });
            //EventBusProvider.PublishQueueAsync("abc", new List<string>() { "7", "8", "9", "10" });
            //EventBusProvider.PublishQueueAsync("abc", new List<string>() { "11", "12", "13", "14", "15" });

            return View();
        }

        
        public async Task<IActionResult> Privacy()
        {
            //第一次获取值 生成的key是  aaa
            var v1 = TestService.GetByKey();

            //生成的key是 bbb_1，占位符被替换：bbb_{userId} => bbb_1  
            var v2 = TestService.GetByKeyAndParamter(1);

            //生成的key是 ccc_1000_1，占位符被替换：ccc_{req:Id}_{type} => ccc_1000_1
            var v3 = await TestService.GetUserInfo(1, new Req() { Id = 1000 });


            //直接在类的方法上加标记，但是方法必须加 virtual
            //生成的key是  TestSingleClassKey
            var v4 = TestSingleClass.Get();


            //第二次获取值
            var v1New = TestService.GetByKey();
            
            var v2New = TestService.GetByKeyAndParamter(1);

            var v3New = await TestService.GetUserInfo(1, new Req() { Id = 1000 });

            var v4New = TestSingleClass.Get();


            var sb = new StringBuilder();
            sb.AppendLine($"Get（永不过期）：=> {TestService.Get()}");
            sb.AppendLine($"GetByKey（永不过期）：第一次=> {v1}");
            sb.AppendLine($"GetByKey（永不过期）：第二次=> {v1New}");

            sb.AppendLine($"GetByKeyAndParamter（3秒）：第一次=> {v2}");
            sb.AppendLine($"GetByKeyAndParamter（3秒）：第二次=> {v2New}");

            sb.AppendLine($"GetUserInfo（十分钟）：第一次=> {Newtonsoft.Json.JsonConvert.SerializeObject(v3)}");
            sb.AppendLine($"GetUserInfo（十分钟）：第二次=> {Newtonsoft.Json.JsonConvert.SerializeObject(v3New)}");

            sb.AppendLine($"TestSingleClass.Get（永不过期）：第一次=> {v4}");
            sb.AppendLine($"TestSingleClass.Get（永不过期）：第二次=> {v4New}");


            var sss = TestService.SetUserInfo(1, new Req() { Id = 1000 });

            TestSingleClass.ClearTestSingleClassCache();


            //await Task.Delay(2000);


            var r1 = TestSingleClass.GetByUserId(1);
            var r2 = TestSingleClass.GetByUserId(2);
            var r3 = TestSingleClass.GetByUserId(3);

            TestSingleClass.ClearByIdList("1,2,3");

            return Content(sb.ToString());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        //[RpcServer("aaaaabbbbb")]
        //public async Task<DateTime> Get()
        //{
        //    return DateTime.Now;
        //}
    }

}
