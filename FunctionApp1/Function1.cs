using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace FunctionApp1;

public class Function1
{
    private readonly ILogger<Function1> _logger;

    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string? Id { get; set; }
        public string? UserName { get; set; }
        public int? Age { get; set; }
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
        _logger.LogInformation("C# Timer trigger add user function executed at: {executionTime}", DateTime.Now);
        await AddUserMethod();

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
    }

    [Function("AddUser")]
    public async Task<IActionResult> AddUser([HttpTrigger(AuthorizationLevel.Anonymous, "post"), FromBody] User newUser)
    {
        if (newUser is null) return new BadRequestObjectResult("Incorrect Data");

        _logger.LogInformation($"\nRequest User : " +
            $"\nUser Name : {newUser.UserName}" +
            $"\nUser Age  : {newUser.Age}");
        await AddUserMethod(newUser);

        return new OkObjectResult("Success");
    }

    [Function("AddUsers")]
    public async Task<IActionResult> AddUsers([HttpTrigger(AuthorizationLevel.Anonymous, "post"), FromBody] List<User> users)
    {
        if (users is null) return new BadRequestObjectResult("Incorrect Data");

        int count = 1;

        foreach (var user in users)
        {
            _logger.LogInformation($"\nRequest User {count} : " +
            $"\nUser Name : {user.UserName}" +
            $"\nUser Age  : {user.Age}");
            await AddUserMethod(user);
            count++;
        }


        return new OkObjectResult("Success");
    }

    [Function("GetUser")]
    public async Task<IActionResult> GetUser([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest _)
    {
        var options = UserDbOptions();

        using (var context = new MyDbContext(options))
        {
            var query = await context.Users.ToListAsync();

            if (query.Count > 0)
            {
                _logger.LogInformation($"Records in In-Memory Database : ");

                foreach (var user in query)
                {
                    _logger.LogInformation(
                        $"\nUser Id\t\t: {user.Id}" +
                        $"\nUser Name\t: {user.UserName}" +
                        $"\nUser Age\t: {user.Age}");
                }
            }
            return new OkObjectResult(query);
        }
    }

    [Function("GetUserAbove")]
    public async Task<IActionResult> GetUserAbove([HttpTrigger(AuthorizationLevel.Anonymous, "get"), FromBody] User nUser)
    {
        var options = UserDbOptions();

        using (var context = new MyDbContext(options))
        {
            var query = from user in context.Users
                        where user.Age >= nUser.Age
                        select user;

            _logger.LogInformation($"\nUser Above {nUser.Age} :");

            foreach (var user in query)
            {
                _logger.LogInformation(
                    $"\nUser Name : {user.UserName}" +
                    $"\nUser Age : {user.Age}");
            }
            return new OkObjectResult(query);
        }
    }

    [Function("DeleteUser")]
    public async Task<IActionResult> DeleteUser([HttpTrigger(AuthorizationLevel.Anonymous, "delete"), FromBody] User user)
    {
        if (user.UserName is null || user.Age is null) return new BadRequestObjectResult("Incorrect Data");

        await DeleteUserMethod(user);

        return new OkObjectResult($"Success Deleting {user.UserName}");
    }

    [Function("DeleteUserTimer")]
    public async Task DeleteUserTimer([TimerTrigger("*/30 * * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger delete user function executed at: {executionTime}", DateTime.Now);
        await DeleteUserMethod();

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
    }

    private Task AddUserMethod()
    {
        var ageRandomizer = new Random();
        User newUser = new()
        {
            UserName = "Wong",
            Age = ageRandomizer.Next(20, 40)
        };

        var options = UserDbOptions();

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

    private Task AddUserMethod(User newUser)
    {
        var options = UserDbOptions();

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

    private Task DeleteUserMethod(User? user = null)
    {
        var options = UserDbOptions();

        using (var context = new MyDbContext(options))
        {

            if (user is null)
            {
                var target = context.Users.First();
                context.Users.Remove(target);
                context.SaveChanges();

                _logger.LogInformation($"\nUser Deleted (Timer) :" +
                    $"\nUser ID : {target.Id}" +
                    $"\nUser Name : {target.UserName}" +
                    $"\nUser Age : {target.Age}");
            }
            else
            {
                context.Users.Remove(user);
                context.SaveChanges();

                _logger.LogInformation($"\nUser Deleted (Request) :" +
                    $"\nUser ID : {user.Id}" +
                    $"\nUser Name : {user.UserName}" +
                    $"\nUser Age : {user.Age}");
            }
        }
        return Task.CompletedTask;
    }

    private DbContextOptions<MyDbContext> UserDbOptions()
    {
        var options = new DbContextOptionsBuilder<MyDbContext>()
            .UseInMemoryDatabase(databaseName: "UserDatabase")
            .Options;

        return options;
    }
}