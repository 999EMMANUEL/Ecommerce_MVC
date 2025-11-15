using Microsoft.Extensions.Options;
using ProyectoEcommerce.Models;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;

namespace ProyectoEcommerce.Services
{
    public class EmailService : IEmailService
    {
        private readonly MailSettings _mailSettings;
        private readonly IPdfService _pdfService;
        private readonly IWebHostEnvironment _env;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public EmailService(
            IOptions<MailSettings> mailSettings,
            IPdfService pdfService,
            IWebHostEnvironment env,
            ICompositeViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider)
        {
            _mailSettings = mailSettings.Value;
            _pdfService = pdfService;
            _env = env;
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task SendInvoiceEmailAsync(Buy buy, string recipientEmail, string recipientName)
        {
            try
            {
                // Generar el HTML de la factura
                string htmlContent = await RenderViewToStringAsync("InvoiceTemplate", buy);

                // Generar el PDF
                byte[] pdfBytes = await _pdfService.GenerateInvoicePdfAsync(buy);

                // Configurar el mensaje
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_mailSettings.SenderEmail, _mailSettings.SenderName);
                    message.To.Add(new MailAddress(recipientEmail, recipientName));
                    message.Subject = $"Factura InnovaTech - Orden #{buy.BuyId}";
                    message.Body = htmlContent;
                    message.IsBodyHtml = true;

                    // Adjuntar el PDF
                    using (var pdfStream = new MemoryStream(pdfBytes))
                    {
                        var attachment = new Attachment(pdfStream, $"Factura_{buy.BuyId}.pdf", "application/pdf");
                        message.Attachments.Add(attachment);

                        // Configurar el cliente SMTP
                        using (var smtpClient = new SmtpClient(_mailSettings.SmtpHost, _mailSettings.SmtpPort))
                        {
                            smtpClient.Credentials = new NetworkCredential(_mailSettings.Username, _mailSettings.Password);
                            smtpClient.EnableSsl = true;

                            await smtpClient.SendMailAsync(message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log del error (puedes usar ILogger aquí)
                throw new Exception($"Error al enviar el email de factura: {ex.Message}", ex);
            }
        }

        private async Task<string> RenderViewToStringAsync(string viewName, object model)
        {
            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

            using (var sw = new StringWriter())
            {
                var viewResult = _viewEngine.FindView(actionContext, $"~/Views/Emails/{viewName}.cshtml", false);

                if (viewResult.View == null)
                {
                    throw new ArgumentNullException($"No se encontró la vista {viewName}");
                }

                var viewDictionary = new ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                {
                    Model = model
                };

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewDictionary,
                    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                    sw,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return sw.ToString();
            }
        }
    }
}