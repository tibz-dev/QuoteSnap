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
    private readonly PdfService _pdfService;
    private readonly InvoiceEmailService _invoiceEmailService;
    public InvoicesController(
        InvoiceService invoiceService,
        PdfService pdfService,
        InvoiceEmailService invoiceEmailService)
    {
        _invoiceService = invoiceService;
        _pdfService = pdfService;
        _invoiceEmailService = invoiceEmailService;
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
                await _invoiceService.RecordPaymentAsync(id,request);

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

    // GET: /api/invoices/{id}/payments
    [HttpGet("{id:guid}/payments")]
    public async Task<ActionResult<List<PaymentDto>>> GetPayments(Guid id)
    {
        try
        {
            var payments = await _invoiceService.GetPaymentsAsync(id);

            return Ok(payments);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
    }

    // GET: /api/invoices/{id}/pdf
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(
        Guid id)
    {
        var result =
            await _pdfService.GenerateInvoiceAsync(id);

        if (result is null)
        {
            return NotFound(new
            {
                message = "Invoice not found."
            });
        }

        return File(
            result.Value.Content,
            "application/pdf",
            result.Value.FileName);
    }

    // POST: /api/invoices/{id}/send-email
    [HttpPost("{id:guid}/send-email")]
    public async Task<IActionResult> SendEmail(
        Guid id,
        SendInvoiceEmailRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _invoiceEmailService.SendAsync(
                    id,
                    request,
                    cancellationToken);

            if (!result.Success)
            {
                return StatusCode(
                    StatusCodes.Status502BadGateway,
                    new
                    {
                        message =
                            "The invoice email could not be sent.",

                        error =
                            result.ErrorMessage
                    });
            }

            return Ok(new
            {
                message =
                    "Invoice email sent successfully.",

                providerMessageId =
                    result.ProviderMessageId
            });
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