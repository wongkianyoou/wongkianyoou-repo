using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace FunctionApp1;

public class Function1
{
    private readonly ILogger<Function1> _logger;

    public class HttpRequestObject
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string RequestId { get; set; }
        public string Headers { get; set; }
        public string Method { get; set; }
        public string Path { get; set; }
        public string Body { get; set; }

    }

    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? Age { get; set; }
    }
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
        }

        public DbSet<HttpRequestObject> Requests { get; set; }

        public DbSet<User> Users { get; set; }

    }

    public Function1(ILogger<Function1> logger)
    {
        _logger = logger;
    }

    [Function("AddRequest")]
    public async Task<IActionResult> AddRequest([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        string requestHeader = JsonConvert.SerializeObject(req.Headers, Formatting.Indented);
        string requestMethod = req.Method;
        string requestPath = req.Path;
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        User user = JsonConvert.DeserializeObject<User>(requestBody);

        _logger.LogInformation($"Request Body : {requestBody}");

        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseInMemoryDatabase(databaseName: "RequestDatabase")
            .Options;

        using (var context = new MyDbContext(options))
        {
            context.Requests.Add(new HttpRequestObject
            {
                Headers = requestHeader,
                Method = requestMethod,
                Path = requestPath,
                Body = requestBody
            });
            context.SaveChanges();

        }

        var name = user.UserName;
        return new OkObjectResult($"Welcome to Azure Functions! by {name}");
    }

    [Function("GetRequest")]
    public async Task<IActionResult> GetRequest([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseInMemoryDatabase(databaseName: "RequestDatabase")
            .Options;

        using (var context = new MyDbContext(options))
        {
            var query = await context.Requests.ToListAsync();

            foreach (var request in query)
            {
                _logger.LogInformation($"Records in In-Memory Database : " +
                    $"\nRecord Header\t: {request.Headers}" +
                    $"\nRecord Method\t: {request.Method}" +
                    $"\nRecord Path\t: {request.Path}" +
                    $"\nRecord Body\t: {request.Body}");
            }

            return new OkObjectResult(query);
        }
    }

    [Function("AddUser")]
    public async Task<IActionResult> AddUser([HttpTrigger(AuthorizationLevel.Anonymous, "post"), FromBody] User newUser)
    {
        if (newUser == null) return new BadRequestResult();

        _logger.LogInformation($"User Name: {newUser.UserName}\nUser Age: {newUser.Age}");

        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseInMemoryDatabase(databaseName: "UserDatabase")
            .Options;

        using (var context = new MyDbContext(options))
        {
            context.Users.Add(new User
            {
                UserId = newUser.UserId,
                UserName = newUser.UserName,
                Age = newUser.Age
            });
            context.SaveChanges();

        }

        return new OkObjectResult(newUser);
    }

    [Function("GetUser")]
    public async Task<IActionResult> GetUser([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseInMemoryDatabase(databaseName: "UserDatabase")
            .Options;

        using (var context = new MyDbContext(options))
        {
            var query = await context.Users.ToListAsync();

            foreach (var user in query)
            {
                _logger.LogInformation($"Records in In-Memory Database : " +
                    $"\nUser Id\t\t: {user.UserId}" +
                    $"\nUser Name\t: {user.UserName}" +
                    $"\nUser Age\t: {user.Age}");
            }

            return new OkObjectResult(query);
        }
    }

}