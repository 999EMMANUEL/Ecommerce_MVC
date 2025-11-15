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
using Microsoft.Extensions.Logging;

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
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<MailSettings> mailSettings,
            IPdfService pdfService,
            IWebHostEnvironment env,
            ICompositeViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider,
            ILogger<EmailService> logger)
        {
            _mailSettings = mailSettings.Value;
            _pdfService = pdfService;
            _env = env;
            _viewEngine = viewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task SendInvoiceEmailAsync(Buy buy, string recipientEmail, string recipientName)
        {
            _logger.LogInformation("Iniciando envío de factura por email para orden {BuyId} a {Email}", buy.BuyId, recipientEmail);

            try
            {
                // Validar parámetros de entrada
                if (buy == null)
                {
                    _logger.LogError("El objeto Buy es null");
                    throw new ArgumentNullException(nameof(buy), "El objeto Buy no puede ser null");
                }

                if (string.IsNullOrWhiteSpace(recipientEmail))
                {
                    _logger.LogError("El email del destinatario está vacío");
                    throw new ArgumentException("El email del destinatario no puede estar vacío", nameof(recipientEmail));
                }

                // Validar que la compra tenga los datos necesarios
                if (buy.Items == null || !buy.Items.Any())
                {
                    _logger.LogError("La orden {BuyId} no tiene items cargados", buy.BuyId);
                    throw new InvalidOperationException("La orden no tiene items. Asegúrese de cargar la relación Items con Include()");
                }

                if (buy.Customer == null)
                {
                    _logger.LogError("La orden {BuyId} no tiene el Customer cargado", buy.BuyId);
                    throw new InvalidOperationException("La orden no tiene Customer cargado. Asegúrese de cargar la relación Customer con Include()");
                }

                // Validar configuración de email
                if (string.IsNullOrEmpty(_mailSettings.SmtpHost) || string.IsNullOrEmpty(_mailSettings.Username))
                {
                    _logger.LogError("Configuración de email incompleta. SmtpHost: {Host}, Username: {User}",
                        _mailSettings.SmtpHost ?? "(null)", _mailSettings.Username ?? "(null)");
                    throw new Exception("La configuración de email no está completa en appsettings.json");
                }

                _logger.LogInformation("Configuración de email: Host={Host}, Port={Port}, Usuario={User}, Email={Email}",
                    _mailSettings.SmtpHost, _mailSettings.SmtpPort, _mailSettings.Username, _mailSettings.SenderEmail);

                _logger.LogDebug("Generando HTML de la factura...");
                // Generar el HTML de la factura
                string htmlContent = await RenderViewToStringAsync("InvoiceTemplate", buy);
                _logger.LogDebug("HTML generado exitosamente. Longitud: {Length} caracteres", htmlContent.Length);

                _logger.LogDebug("Generando PDF de la factura...");
                // Generar el PDF
                byte[] pdfBytes = await _pdfService.GenerateInvoicePdfAsync(buy);
                _logger.LogDebug("PDF generado exitosamente. Tamaño: {Size} bytes", pdfBytes.Length);

                // Configurar el mensaje
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_mailSettings.SenderEmail, _mailSettings.SenderName);
                    message.To.Add(new MailAddress(recipientEmail, recipientName));
                    message.Subject = $"Factura InnovaTech - Orden #{buy.BuyId}";
                    message.Body = htmlContent;
                    message.IsBodyHtml = true;

                    _logger.LogDebug("Mensaje de email configurado. De: {From}, Para: {To}, Asunto: {Subject}",
                        message.From.Address, recipientEmail, message.Subject);

                    // Crear el attachment SIN disponer el stream antes de enviar
                    var pdfStream = new MemoryStream(pdfBytes);
                    try
                    {
                        var attachment = new Attachment(pdfStream, $"Factura_{buy.BuyId}.pdf", "application/pdf");
                        message.Attachments.Add(attachment);

                        _logger.LogDebug("PDF adjuntado al mensaje. Nombre: Factura_{BuyId}.pdf", buy.BuyId);

                        // Configurar el cliente SMTP
                        using (var smtpClient = new SmtpClient(_mailSettings.SmtpHost, _mailSettings.SmtpPort))
                        {
                            smtpClient.Credentials = new NetworkCredential(_mailSettings.Username, _mailSettings.Password);
                            smtpClient.EnableSsl = true;
                            smtpClient.Timeout = 30000; // 30 segundos timeout

                            _logger.LogInformation("Enviando email a través de SMTP {Host}:{Port}...",
                                _mailSettings.SmtpHost, _mailSettings.SmtpPort);

                            await smtpClient.SendMailAsync(message);

                            _logger.LogInformation("Email enviado exitosamente a {Email} para orden {BuyId}",
                                recipientEmail, buy.BuyId);
                        }
                    }
                    finally
                    {
                        // Asegurar que el stream se dispose después de enviar
                        pdfStream?.Dispose();
                    }
                }
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "Error SMTP al enviar email a {Email}. StatusCode: {StatusCode}",
                    recipientEmail, smtpEx.StatusCode);
                throw new Exception($"Error SMTP al enviar el email: {smtpEx.Message}. StatusCode: {smtpEx.StatusCode}", smtpEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar el email de factura a {Email} para orden {BuyId}",
                    recipientEmail, buy.BuyId);
                throw new Exception($"Error al enviar el email de factura: {ex.Message}", ex);
            }
        }

        private async Task<string> RenderViewToStringAsync(string viewName, object model)
        {
            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

            using (var sw = new StringWriter())
            {
                var viewPath = $"~/Views/Emails/{viewName}.cshtml";
                var viewResult = _viewEngine.GetView(executingFilePath: null, viewPath: viewPath, isMainPage: true);

                if (!viewResult.Success)
                {
                    var searchedLocations = string.Join(Environment.NewLine, viewResult.SearchedLocations);
                    _logger.LogError("No se encontró la vista {ViewName}. Ubicaciones buscadas:{NewLine}{SearchedLocations}",
                        viewName, Environment.NewLine, searchedLocations);
                    throw new FileNotFoundException($"No se encontró la vista {viewName}. Ubicaciones buscadas: {searchedLocations}");
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