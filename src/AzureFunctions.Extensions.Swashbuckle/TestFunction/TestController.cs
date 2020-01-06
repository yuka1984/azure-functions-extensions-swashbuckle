using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Newtonsoft.Json;

namespace TestFunction
{
    /// <summary>
    /// テストコントローラ
    /// </summary>
    [ApiExplorerSettings(GroupName = "testee")]
    public class TestController
    {
        /// <summary>
        /// すべてのテストの取得
        /// </summary>
        /// <param name="request"></param>
        /// <returns>すべてのテスト</returns>
        [ProducesResponseType(typeof(TestModel[]), (int)HttpStatusCode.OK)]
        [FunctionName("TestGets")]
        [QueryStringParameter("expand", "it is expand parameter", DataType = typeof(int))]
        public async Task<IActionResult> Gets([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "test")] HttpRequest request)
        {
            return new OkObjectResult(new[] {new TestModel(), new TestModel(),});
        }

        /// <summary>
        /// テストの取得
        /// </summary>
        /// <param name="request"></param>
        /// <param name="id">テストId</param>
        /// <returns>指定されたテスト</returns>
        [ProducesResponseType(typeof(TestModel), (int)HttpStatusCode.OK)]
        [FunctionName("TestGet")]
        public Task<IActionResult> Get([HttpTrigger(AuthorizationLevel.Function, "get", Route = "test/{id}")]
            HttpRequest request, int id)
        {
            return Task.FromResult<IActionResult>(new OkObjectResult(new TestModel()));
        }

        /// <summary>
        /// テストの取得
        /// </summary>
        /// <param name="request"></param>
        /// <param name="id">テストId</param>
        /// <returns>指定されたテスト</returns>
        [ProducesResponseType(typeof(TestModel), (int)HttpStatusCode.OK)]
        [QueryStringParameter("name", "this is name", DataType = typeof(string), Required = true)]
        [FunctionName("TestGetCat")]
        public Task<IActionResult> GetCat([HttpTrigger(AuthorizationLevel.Function, "get", Route = "cat/{id}/{testId?}")]
            HttpRequest request, int id, int? testId)
        {
            return Task.FromResult<IActionResult>(new OkObjectResult(new TestModel()));
        }

        /// <summary>
        /// テストの追加
        /// </summary>
        /// <param name="testModel">テストモデル</param>
        /// <returns>追加結果</returns>
        [ProducesResponseType(typeof(TestModel), (int)HttpStatusCode.Created)]
        [FunctionName("TestAdd")]
        public Task<IActionResult> Add([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "test")]TestModel testModel)
        {
            return Task.FromResult<IActionResult>(new CreatedResult("", testModel));
        }

        /// <summary>
        /// テストの追加と検証
        /// </summary>
        /// <param name="testModel">テストモデル</param>
        /// <returns>追加結果</returns>
        [ProducesResponseType(typeof(TestModel), (int)HttpStatusCode.Created)]
        [QueryStringParameter("test", "test", Required = false)]
        [FunctionName("TestAddGet")]
        public async Task<IActionResult> AddAndGet([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "testandget")]HttpRequest httpRequest)
        {
            if (httpRequest.Method.ToLower() == "post")
            {
                using (var reader = new StreamReader(httpRequest.Body))
                {
                    var json = await reader.ReadToEndAsync();
                    var testModel = JsonConvert.DeserializeObject<TestModel>(json);
                    return new CreatedResult("", testModel);
                }
            }

            return new OkResult();
        }
        
        /// <summary>
        /// Test array query parameters.
        /// </summary>
        /// <param name="request">The request.</param>
        [ProducesResponseType(typeof(TestModel[]), (int)HttpStatusCode.OK)]
        [FunctionName("TestGetsArrayQueryParam")]
        [QueryStringParameter("intList", "List of int", DataType = typeof(IEnumerable<int>), Required = false)]
        [QueryStringParameter("stringList", "List of string", DataType = typeof(List<string>), Required = false)]
        [QueryStringParameter("dateTimeList", "List of DateTime", DataType = typeof(DateTime[]), Required = false)]
        [QueryStringParameter("guidList", "List of GUID", DataType = typeof(IList<Guid>), Required = false)]
        [QueryStringParameter("defaultTypeParameter", "Default parameter", Required = false)]
        [QueryStringParameter("intParameter", "Int parameter", DataType = typeof(int), Required = false)]
        [QueryStringParameter("stringParameter", "String parameter", DataType = typeof(string), Required = false)]
        [QueryStringParameter("dateTimeParameter", "DateTime parameter", DataType = typeof(DateTime), Required = false)]
        [QueryStringParameter("guidParameter", "Guid parameter", DataType = typeof(Guid), Required = false)]
        public async Task<IActionResult> GetsArrayQueryParam([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "testarray")] HttpRequest request)
        {
            var intList = request.Query["intList"].ToArray();
            var stringList = request.Query["stringList"].ToArray();
            var dateTimeList = request.Query["dateTimeList"].ToArray();
            var defaultTypeParameter = request.Query["defaultTypeParameter"];
            var intParameter = request.Query["intParameter"];
            var stringParameter = request.Query["stringParameter"];
            return new OkObjectResult(new[] { new TestModel(), new TestModel(), });
        }

        /// <summary>
        /// テストモデル
        /// </summary>
        public class TestModel
        {
            /// <summary>
            /// Id
            /// </summary>
            [Required]
            public int Id { get; set; }

            /// <summary>
            /// 名前
            /// </summary>
            [Required]
            [MaxLength(512)]
            public string Name { get; set; }

            /// <summary>
            /// 詳細説明
            /// </summary>
            [MaxLength(10240)]
            public string Description { get; set; }
        }
    }
}
