using ictis.schedule.core;
using Ictis.Schedule.Services.ScheduleService;
using Ictis.Schedule.Services.TelegramService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ictis.schedule.tgbot;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var serviceCollection = RegisterServices();
        var telegramService = serviceCollection.GetRequiredService<ITelegramService>();
        try
        {
            await telegramService.GetMe();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static ServiceProvider RegisterServices()
    {
        var config = DotEnvLoad.Load();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IConfiguration>(config)
            .AddScoped<IScheduleService, ScheduleService>()
            .AddScoped<ITelegramService, TelegramService>()
            .BuildServiceProvider();
        return serviceProvider;
    }
}