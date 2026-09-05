using Microsoft.Extensions.Hosting;
using Serilog;

namespace Starlight.Console;

public sealed class ConsoleService(
    CommandRegistry registry,
    InteractiveConsole console,
    IHostApplicationLifetime lifetime
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Without an interactive console (systemd/Docker/redirected stdin) there is
        // nothing to read, and a blocking read here would stall graceful shutdown.
        if (System.Console.IsInputRedirected)
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            var input = await console.ReadLineAsync(stoppingToken);

            // Cancellation requested (shutdown) or end of input stream.
            if (input is null)
                break;

            if (string.IsNullOrWhiteSpace(input))
                continue;

            var parts = input.Split(separator: ' ', StringSplitOptions.RemoveEmptyEntries);
            var name = parts[0];
            var args = parts[1..];

            if (!registry.TryGet(name, out var command))
            {
                Log.Warning("Unknown command: {Command}", name);
                continue;
            }

            try
            {
                await command.ExecuteAsync(args, stoppingToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Command failed: {Command}", name);
            }
        }
    }
}
