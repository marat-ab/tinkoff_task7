using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Task7.Clients;

namespace Task7.Handlers;

public class Handler : IHandler
{
    private readonly int _generalTimeoutInMs = 15000;

    private readonly IClient _client1;
    private readonly IClient _client2;
    private readonly ILogger<Handler> _logger;

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly CancellationToken _token;

    private int _retriesCount = 0;

    public Handler(
      IClient client1,
      IClient client2,
      ILogger<Handler> logger)
    {
        _client1 = client1;
        _client2 = client2;
        _logger = logger;

        _cancellationTokenSource = new CancellationTokenSource();
        _token = _cancellationTokenSource.Token;
    }
    
    public async Task<IApplicationStatus> GetApplicationStatus(string id)
    {
        var timer = new System.Timers.Timer(_generalTimeoutInMs);
        timer.Elapsed += OnTimedEvent;
        timer.Start();

        var client1Task = GetApplicationStatusWithTimeout(_client1, id, new TimeSpan(0), _token);
        var client2Task = GetApplicationStatusWithTimeout(_client2, id, new TimeSpan(0), _token);
                
        while (!_token.IsCancellationRequested)
        {
            try
            {
                await Task.WhenAny(new Task[] { client1Task, client2Task })
                    .WaitAsync(TimeSpan.FromMilliseconds(_generalTimeoutInMs), _token);

                if (client1Task.IsCompleted)
                {
                    (var client1Status, client1Task) = ProcessingClientResponse(client1Task.Result, id);

                    if (client1Status != null)
                        return client1Status;
                }

                if (client2Task.IsCompleted)
                {
                    (var client2Status, client2Task) = ProcessingClientResponse(client2Task.Result, id);

                    if (client2Status != null)
                        return client2Status;
                }
            }
            catch(OperationCanceledException ex)
            {
                _logger.LogInformation(exception: ex, message: ex.Message);
            }
        }

        // TODO: Лучше создать IDateTimeService, чтобы можно было использовать DI
        var dt = DateTime.UtcNow;

        var result = (IApplicationStatus)new FailureStatus(dt, _retriesCount);
        return result;
    }

    // Private
    private async Task<IResponse> GetApplicationStatusWithTimeout(
        IClient client, 
        string id, 
        TimeSpan timeout, 
        CancellationToken token)
    {
        await Task.Delay(timeout, token);
        var result = await client.GetApplicationStatus(id, token);

        return result;
    }

    private (IApplicationStatus? Status, Task<IResponse>? ClientTask) ProcessingClientResponse(
        IResponse response,
        string id)
    {        
        if (response is SuccessResponse successResponse)
        {
            var result = (IApplicationStatus)new SuccessStatus(successResponse.Id, successResponse.Status);

            return (result, null);
        }
        else if (response is FailureResponse)
        {
            // TODO: Лучше создать IDateTimeService, чтобы можно было использовать DI
            var dt = DateTime.UtcNow;

            var result = (IApplicationStatus)new FailureStatus(dt, _retriesCount);

            return (result, null);
        }
        else if (response is RetryResponse retryResponse)
        {
            Interlocked.Increment(ref _retriesCount);

            var timeout = retryResponse.Delay;
            var clientTaskOut = GetApplicationStatusWithTimeout(_client1, id, timeout, _token);

            return (null, clientTaskOut);
        }

        throw new Exception("Unknown client task result");
    }

    private void OnTimedEvent(Object source, ElapsedEventArgs e)
    {
        _cancellationTokenSource.Cancel();
    }
}