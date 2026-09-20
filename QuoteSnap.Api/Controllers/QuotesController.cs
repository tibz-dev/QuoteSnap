using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuoteSnap.Application.Quotes;
using QuoteSnap.Infrastructure.Services;

namespace QuoteSnap.Api.Controllers;

[ApiController]
[Route("api/quotes")]
[Authorize]
public class QuotesController : ControllerBase
{
    private readonly QuoteService _quoteService;

    public QuotesController(
        QuoteService quoteService)
    {
        _quoteService = quoteService;
    }

    // GET: /api/quotes
    [HttpGet]
    public async Task<ActionResult<List<QuoteDto>>> GetAll()
    {
        var quotes =
            await _quoteService.GetAllAsync();

        return Ok(quotes);
    }

    // GET: /api/quotes/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuoteDto>> GetById(
        Guid id)
    {
        var quote =
            await _quoteService.GetByIdAsync(id);

        if (quote is null)
        {
            return NotFound(new
            {
                message = "Quote not found."
            });
        }

        return Ok(quote);
    }

    // POST: /api/quotes
    [HttpPost]
    public async Task<ActionResult<QuoteDto>> Create(
        CreateQuoteRequest request)
    {
        try
        {
            var quote =
                await _quoteService.CreateAsync(request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = quote.Id },
                quote);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }
}