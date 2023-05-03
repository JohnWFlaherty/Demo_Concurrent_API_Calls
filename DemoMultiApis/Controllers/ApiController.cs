
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace DemoMultiApis.Controllers;

[ApiController]
[Route("[controller]")]
public class ApiController : ControllerBase
{
    private readonly ILogger<ApiController> _logger;

    public ApiController(ILogger<ApiController> logger)
    {
        _logger = logger;
    }

    private string RequestBaseAddress => $"{Request.Scheme}://{Request.Host}";

    private Task<RestResponse<int>> DoGetTask(RestClient client, string resource, CancellationToken cancellationToken)
        => client.ExecuteAsync<int>(new RestRequest(resource, Method.Get), cancellationToken);

    private bool LogAnyError(
        string apiName,
        IEnumerable<RestResponse<int>> responses)
    {
        var result = false;

        foreach (var response in responses)
        {
            if (!response.IsSuccessful)
            {
                result = true;
                _logger.LogError(
                    response.ErrorException,
                    "Failed {API}: calling {SERVICE} - {MSG}",
                    apiName,
                    response.Request?.Resource,
                    response.ErrorMessage);
            }
        }

        return result;
    }

    private async Task<IActionResult> ExecuteApi1TasksAsync(Func<RestClient, CancellationToken, IList<Task<RestResponse<int>>>> createTasks)
    {
        try
        {
            using var client = new RestClient(RequestBaseAddress);
            using var cancellationTokenSource = new CancellationTokenSource();
            var requests = createTasks(client, cancellationTokenSource.Token);
            cancellationTokenSource.CancelAfter(900);
            await Task.WhenAll(requests);

            if (!LogAnyError(nameof(Get1Async), requests.Select(r => r.Result)))
            {
                return Ok(new Api1Response(requests[0].Result.Data, requests[1].Result.Data));
            }
        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed {API} request.", nameof(Get1Async));
        }

        return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
    }

    [HttpGet("1")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Api1Response))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get1Async()
    {
        return await ExecuteApi1TasksAsync(
            (client, token) => new List<Task<RestResponse<int>>>
            {
                DoGetTask(client, "api/2", token),
                DoGetTask(client, "api/3", token),
            });
    }

    private Task<RestResponse<int>> DoPostTask<T>(
        RestClient client,
        string resource,
        T body,
        CancellationToken cancellationToken)
        where T : notnull
    {
        var request = new RestRequest(resource, Method.Post);
        request.AddBody(body);
        return client.ExecuteAsync<int>(request, cancellationToken);
    }

    [HttpPost("1")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Api1Response))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post1Async([FromBody] Api1PostRequest postRequest)
    {
        return await ExecuteApi1TasksAsync(
            (client, token) => new List<Task<RestResponse<int>>>
            {
                DoPostTask(client, "api/2", postRequest.Api2Delay, token),
                DoPostTask(client, "api/3", postRequest.Api3Delay, token),
            });
    }

    private async Task<IActionResult> LongRunningOperation(int delay, string actionName)
    {
        try
        {
            await Task.Delay(delay);
            return Ok(delay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed {API} request.", actionName);
        }

        return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
    }

    [HttpGet("2")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get2Async() => await LongRunningOperation(new Random().Next(50, 1000), nameof(Get2Async));

    [HttpGet("3")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get3Async() => await LongRunningOperation(new Random().Next(50, 1000), nameof(Get3Async));

    [HttpPost("2")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post2Async([FromBody] int delay)
        => await LongRunningOperation(delay, nameof(Get2Async));

    [HttpPost("3")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post3Async([FromBody] int delay)
        => await LongRunningOperation(delay, nameof(Get3Async));
}