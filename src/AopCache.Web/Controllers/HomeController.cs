using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AopCache.Web.Models;

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


            var model = new UserInfo()
            {
                Id = 100,
                Name = "aa",
                UserInfo2 = new UserInfo2()
                {
                    Id = 111,
                    Name = "bb"
                }
            };

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
            sb.AppendLine($"GetByKey（永不过期）：第一次=> {v1}");
            sb.AppendLine($"GetByKey（永不过期）：第二次=> {v1New}");

            sb.AppendLine($"GetByKeyAndParamter（3秒）：第一次=> {v2}");
            sb.AppendLine($"GetByKeyAndParamter（3秒）：第二次=> {v2New}");

            sb.AppendLine($"GetUserInfo（十分钟）：第一次=> {Newtonsoft.Json.JsonConvert.SerializeObject(v3)}");
            sb.AppendLine($"GetUserInfo（十分钟）：第二次=> {Newtonsoft.Json.JsonConvert.SerializeObject(v3New)}");

            sb.AppendLine($"TestSingleClass.Get（永不过期）：第一次=> {v4}");
            sb.AppendLine($"TestSingleClass.Get（永不过期）：第二次=> {v4New}");

            return Content(sb.ToString());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
