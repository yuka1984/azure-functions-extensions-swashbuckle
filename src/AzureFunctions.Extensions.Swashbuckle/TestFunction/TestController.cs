﻿using System;
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
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

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
        public async Task<IActionResult> AddAndGet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "testandget")]
            [RequestBodyType(typeof(TestModel), "The request body", name: "Payload")]HttpRequest httpRequest)
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

        public class TypeExcludeFilter : ISchemaFilter
        {
            private HashSet<Type> _types;

            public class Options
            {
                public Options(params  Type[] types)
                {
                    Types = types;
                }

                public Type[] Types { get; }
            }
            
            public TypeExcludeFilter(Options options)
            {
                _types = new HashSet<Type>(options.Types);
            }
            
            public void Apply(OpenApiSchema schema, SchemaFilterContext context)
            {
                if (schema?.Properties == null || context.Type == null)
                    return;

                foreach (var type in _types)
                {
                    context.SchemaRepository.Schemas.Remove(type.Name);
                }
                
                var excludedProperties = context.Type.GetProperties()
                    .Where(t => _types.Contains(t.PropertyType)).ToArray();
                
                foreach (var excludedProperty in excludedProperties)
                {
                    if (schema.Properties.ContainsKey(excludedProperty.Name))
                        schema.Properties.Remove(excludedProperty.Name);
                }
            }
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
            
            [JsonIgnore]
            public IModelValidator Validator { get; set; }
        }
    }
}
