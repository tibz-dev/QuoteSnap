using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuoteSnap.Application.Invoices;
using QuoteSnap.Infrastructure.Services;

namespace QuoteSnap.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoicesController : ControllerBase
{
    private readonly InvoiceService _invoiceService;

    public InvoicesController(
        InvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    // GET: /api/invoices
    [HttpGet]
    public async Task<ActionResult<List<InvoiceDto>>> GetAll()
    {
        var invoices =
            await _invoiceService.GetAllAsync();

        return Ok(invoices);
    }

    // GET: /api/invoices/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetById(
        Guid id)
    {
        var invoice =
            await _invoiceService.GetByIdAsync(id);

        if (invoice is null)
        {
            return NotFound(new
            {
                message = "Invoice not found."
            });
        }

        return Ok(invoice);
    }

    // POST: /api/invoices/from-quote/{quoteId}
    [HttpPost("from-quote/{quoteId:guid}")]
    public async Task<ActionResult<InvoiceDto>> ConvertQuote(
        Guid quoteId,
        ConvertQuoteToInvoiceRequest request)
    {
        try
        {
            var invoice =
                await _invoiceService.ConvertQuoteAsync(
                    quoteId,
                    request);

            return CreatedAtAction(
                nameof(GetById),
                new { id = invoice.Id },
                invoice);
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

    // POST: /api/invoices/{id}/payments
    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<InvoiceDto>> RecordPayment(
        Guid id,
        RecordPaymentRequest request)
    {
        try
        {
            var invoice =
                await _invoiceService.RecordPaymentAsync(
                    id,
                    request);

            if (invoice is null)
            {
                return NotFound(new
                {
                    message = "Invoice not found."
                });
            }

            return Ok(invoice);
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
}