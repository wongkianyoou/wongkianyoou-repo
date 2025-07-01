using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FunctionApp1;

public class Function1
{
    private readonly ILogger<Function1> _logger;

    public class HttpRequestObject
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string requestId { get; set; }
        public string headers { get; set; }
        public string method { get; set; }
        public string path { get; set; }
        public string body { get; set; }

    }
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
        }

        public DbSet<HttpRequestObject> Requests { get; set; }

    }

    public Function1(ILogger<Function1> logger)
    {
        _logger = logger;
    }

    [Function("Function1")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        string requestHeader = JsonConvert.SerializeObject(req.Headers, Formatting.Indented);
        string requestMethod = req.Method;
        string requestPath = req.Path;
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        _logger.LogInformation($"Request Body : {requestBody}");

        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseInMemoryDatabase(databaseName: "RequestDatabase")
            .Options;

        using (var context = new MyDbContext(options))
        {
            context.Requests.Add(new HttpRequestObject
            {
                headers = requestHeader,
                method = requestMethod,
                path = requestPath,
                body = requestBody
            });
            context.SaveChanges();

            var query = await context.Requests.ToListAsync();

            foreach (var request in query)
            {
                _logger.LogInformation($"Records in In-Memory Database : " +
                    $"\nRecord Header\t: {JsonConvert.SerializeObject(request.headers)}" +
                    $"\nRecord Method\t: {request.method}" +
                    $"\nRecord Path\t: {request.path}" +
                    $"\nRecord Body\t: {request.body}");
            }

        }

        string name = "Wong Kian Yoou";
        return new OkObjectResult($"Welcome to Azure Functions! by {name}");
    }
}