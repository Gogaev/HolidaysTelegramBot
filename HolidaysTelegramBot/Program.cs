using HolidaysTelegramBot;
using HolidaysTelegramBot.Abstract;
using HolidaysTelegramBot.Options;
using HolidaysTelegramBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Services;

// var token = Environment.GetEnvironmentVariable("BotConfiguration__Token");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration.GetSection("BotConfiguration");
        services.Configure<BotOptions>(configuration);

        var botOptions = new BotOptions();
        configuration.Bind(botOptions);

        services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    TelegramBotClientOptions options = new(botOptions.Token);
                    return new TelegramBotClient(options, httpClient);
                });

        // "Host=localhost;Port=5432;Database=bot;Username=postgres;Password=password;Integrated Security=false;"
        var connectionString = context.Configuration["DbConnectionString"];
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddScoped<IChatGptService, ChatGptService>();
        services.AddHostedService<PollingService>();
    })
    .Build();


await host.RunAsync();