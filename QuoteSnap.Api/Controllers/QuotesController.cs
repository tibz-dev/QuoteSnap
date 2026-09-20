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
    private readonly PdfService _pdfService;

    public QuotesController(
        QuoteService quoteService,
        PdfService pdfService)
    {
        _quoteService = quoteService;
        _pdfService = pdfService;
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

    // PATCH: /api/quotes/{id}/status
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<QuoteDto>> UpdateStatus(
        Guid id,
        UpdateQuoteStatusRequest request)
    {
        try
        {
            var quote =
                await _quoteService.UpdateStatusAsync(
                    id,
                    request.Status);

            if (quote is null)
            {
                return NotFound(new
                {
                    message = "Quote not found."
                });
            }

            return Ok(quote);
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
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // GET: /api/quotes/{id}/pdf
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(
        Guid id)
    {
        var result =
            await _pdfService.GenerateQuoteAsync(id);

        if (result is null)
        {
            return NotFound(new
            {
                message = "Quote not found."
            });
        }

        return File(
            result.Value.Content,
            "application/pdf",
            result.Value.FileName);
    }
}