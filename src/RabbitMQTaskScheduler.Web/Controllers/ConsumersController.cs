using Microsoft.AspNetCore.Mvc;
using RabbitMQTaskScheduler.Core.Interfaces;
using RabbitMQTaskScheduler.Core.Models;
using RabbitMQTaskScheduler.Web.Models;

namespace RabbitMQTaskScheduler.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConsumersController : ControllerBase
{
    private readonly IConsumerManager _consumerManager;
    private readonly ILogger<ConsumersController> _logger;

    public ConsumersController(
        IConsumerManager consumerManager,
        ILogger<ConsumersController> logger)
    {
        _consumerManager = consumerManager;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConsumerInfo>>> GetConsumers()
    {
        try
        {
            var consumers = await _consumerManager.GetActiveConsumersAsync();
            return Ok(consumers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving consumers");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("count")]
    public async Task<ActionResult<int>> GetConsumerCount()
    {
        try
        {
            var count = await _consumerManager.GetActiveConsumerCountAsync();
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving consumer count");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("scale")]
    public async Task<ActionResult> ScaleConsumers([FromBody] ScaleConsumersRequest request)
    {
        try
        {
            await _consumerManager.ScaleConsumersAsync(request.TargetCount);
            return Ok(new { Message = $"Scaling request sent for {request.TargetCount} consumers" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scaling consumers to {TargetCount}", request.TargetCount);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{consumerId}")]
    public async Task<ActionResult> UnregisterConsumer(string consumerId)
    {
        try
        {
            await _consumerManager.UnregisterConsumerAsync(consumerId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unregistering consumer {ConsumerId}", consumerId);
            return StatusCode(500, "Internal server error");
        }
    }
}