using System.Reflection;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace repro_mass_transit_interface_property_inits;

public interface ITestMessage
{
    public int Prop { get; init; }
}

public class TestMessage : ITestMessage
{
    public int Prop { get; init; }
}

public class TestMessageConsumer : IConsumer<ITestMessage>
{
    private readonly ILogger<TestMessageConsumer> _logger;

    public TestMessageConsumer(ILogger<TestMessageConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ITestMessage> context)
    {
        _logger.LogInformation("Consumed.");
        return Task.CompletedTask;
    }
}

public class TestPublisherService : IHostedService
{
    private readonly IPublishEndpoint _publisher;

    public TestPublisherService(IPublishEndpoint publisher)
    {
        _publisher = publisher;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await _publisher.Publish<ITestMessage>(new TestMessage { Prop = 25 }, cancellationToken);
            await Task.Delay(10000, cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        await CreateHostBuilder(args).Build().RunAsync();
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logger => logger.SetMinimumLevel(LogLevel.Trace))
            .ConfigureServices((hostContext, services) =>
            {
                services.AddMassTransit(x =>
                {
                    x.SetKebabCaseEndpointNameFormatter();
                    x.SetInMemorySagaRepositoryProvider();
                    x.AddConsumers(Assembly.GetExecutingAssembly());

                    x.UsingInMemory((context, cfg) => { cfg.ConfigureEndpoints(context); });
                });
                    
                services.AddHostedService<TestPublisherService>();
            });
}