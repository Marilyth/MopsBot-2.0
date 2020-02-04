using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

// Change this namespace if desired
namespace MopsBot
{
    // This service requires that your bot is being run by a daemon that handles
    // Exit Code 1 (or any exit code) as a restart.
    //
    // If you do not have your bot setup to run in a daemon, this service will just
    // terminate the process and the bot will not restart.
    // 
    // Links to daemons:
    // [Powershell (Windows+Unix)] https://gitlab.com/snippets/21444
    // [Bash (Unix)] https://stackoverflow.com/a/697064
    public class ReliabilityService
    {
        // --- Begin Configuration Section ---
        // How long should we wait on the client to reconnect before resetting?
        private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

        // Should we attempt to reset the client? Set this to false if your client is still locking up.
        private static readonly bool _attemptReset = true;

        // Change log levels if desired:
        private static readonly LogSeverity _debug = LogSeverity.Debug;
        private static readonly LogSeverity _info = LogSeverity.Info;
        private static readonly LogSeverity _critical = LogSeverity.Critical;
        // --- End Configuration Section ---

        private readonly DiscordSocketClient[] _discord;
        private readonly Func<LogMessage, Task> _logger;
        private CancellationTokenSource _cts;

        public ReliabilityService(DiscordShardedClient discord, Func<LogMessage, Task> logger = null)
        {
            _cts = new CancellationTokenSource();
            _discord = (DiscordSocketClient[])discord.Shards;
            _logger = logger ?? (_ => Task.CompletedTask);

            foreach (var shard in _discord)
            {
                shard.Connected += () => ConnectedAsync(shard);
                shard.Disconnected += x => DisconnectedAsync(shard, x);
            }
        }

        public Task ConnectedAsync(DiscordSocketClient client)
        {
            // Cancel all previous state checks and reset the CancelToken - client is back online
            _ = DebugAsync($"Shard {client.ShardId} reconnected, resetting cancel tokens...");
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            _ = DebugAsync($"Shard {client.ShardId} reconnected, cancel tokens reset.");

            return Task.CompletedTask;
        }

        public Task DisconnectedAsync(DiscordSocketClient client, Exception _e)
        {
            // Check the state after <timeout> to see if we reconnected
            _ = InfoAsync($"Shard {client.ShardId} disconnected, starting timeout task...");
            _ = Task.Delay(_timeout, _cts.Token).ContinueWith(async _ =>
            {
                await DebugAsync("Timeout expired, continuing to check client state...");
                await CheckStateAsync(client);
                await DebugAsync("State came back okay");
            });

            return Task.CompletedTask;
        }

        private async Task CheckStateAsync(DiscordSocketClient client)
        {
            // Client reconnected, no need to reset
                if (client.ConnectionState == ConnectionState.Connected) return;
                if (_attemptReset)
                {
                    await InfoAsync("Attempting to reset the client");

                    var timeout = Task.Delay(_timeout);
                    var connect = client.StartAsync();
                    var task = await Task.WhenAny(timeout, connect);

                    if (task == timeout)
                    {
                        await CriticalAsync($"Shard {client.ShardId} reset timed out (task deadlocked?), killing process");
                        FailFast();
                    }
                    else if (connect.IsFaulted)
                    {
                        await CriticalAsync($"Shard {client.ShardId} reset faulted, killing process", connect.Exception);
                        FailFast();
                    }
                    else if (connect.IsCompletedSuccessfully)
                        await InfoAsync($"Shard {client.ShardId} reset succesfully!");
                    return;
                }

                await CriticalAsync($"Shard {client.ShardId} did not reconnect in time, killing process");
                FailFast();
        }

        private void FailFast()
            => Environment.Exit(1);

        // Logging Helpers
        private const string LogSource = "Reliability";
        private Task DebugAsync(string message)
            => _logger.Invoke(new LogMessage(_debug, LogSource, message));
        private Task InfoAsync(string message)
            => _logger.Invoke(new LogMessage(_info, LogSource, message));
        private Task CriticalAsync(string message, Exception error = null)
            => _logger.Invoke(new LogMessage(_critical, LogSource, message, error));
    }
}