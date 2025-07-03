using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FunctionApp1;

public class Function1
{
    private readonly ILogger<Function1> _logger;

    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string? Id { get; set; }
        public string? UserName { get; set; }
        public string? Age { get; set; }
    }

    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; }
    }

    public Function1(ILogger<Function1> logger)
    {
        _logger = logger;
    }

    [Function("AddUserTimer")]
    public async Task AddUserTimer([TimerTrigger("*/30 * * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);
        await AddUserMethod();

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
    }

    [Function("AddUser")]
    public async Task<IActionResult> AddUser([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData _)
    {
        await AddUserMethod();

        return new OkObjectResult("Success");
    }

    [Function("GetUser")]
    public async Task<IActionResult> GetUser([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest _)
    {
        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseInMemoryDatabase(databaseName: "UserDatabase")
            .Options;

        using (var context = new MyDbContext(options))
        {
            var query = await context.Users.ToListAsync();

            if (query.Count > 0) _logger.LogInformation($"Records in In-Memory Database : ");

            foreach (var user in query)
            {
                _logger.LogInformation(
                    $"\nUser Id\t\t: {user.Id}" +
                    $"\nUser Name\t: {user.UserName}" +
                    $"\nUser Age\t: {user.Age}");
            }

            return new OkObjectResult(query);
        }
    }

    private Task AddUserMethod()
    {
        var ageRandomizer = new Random();
        User newUser = new();
        newUser.UserName = "Wong";
        newUser.Age = ageRandomizer.Next(20, 40).ToString();

        _logger.LogInformation($"User Name: {newUser.UserName}\nUser Age: {newUser.Age}");

        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseInMemoryDatabase(databaseName: "UserDatabase")
            .Options;

        using (var context = new MyDbContext(options))
        {
            context.Users.Add(newUser);
            context.SaveChanges();

            _logger.LogInformation($"\nNew User Added :" +
                $"\nUser Name: {newUser.UserName}" +
                $"\nUser Age : {newUser.Age}");
        }

        return Task.CompletedTask;
    }
}