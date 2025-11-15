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
        private readonly IWebHostEnvironment _env;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<MailSettings> mailSettings,
            IWebHostEnvironment env,
            ICompositeViewEngine viewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider,
            ILogger<EmailService> logger)
        {
            _mailSettings = mailSettings.Value;
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

                // Configurar el mensaje (sin adjunto PDF)
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_mailSettings.SenderEmail, _mailSettings.SenderName);
                    message.To.Add(new MailAddress(recipientEmail, recipientName));
                    message.Subject = $"Factura InnovaTech - Orden #{buy.BuyId}";
                    message.Body = htmlContent;
                    message.IsBodyHtml = true;

                    _logger.LogDebug("Mensaje de email configurado. De: {From}, Para: {To}, Asunto: {Subject}",
                        message.From.Address, recipientEmail, message.Subject);

                    // Configurar el cliente SMTP
                    using (var smtpClient = new SmtpClient(_mailSettings.SmtpHost, _mailSettings.SmtpPort))
                    {
                        smtpClient.Credentials = new NetworkCredential(_mailSettings.Username, _mailSettings.Password);
                        smtpClient.EnableSsl = true;
                        smtpClient.Timeout = 30000; // 30 segundos timeout

                        _logger.LogInformation("Enviando email con factura HTML (sin adjunto PDF) a través de SMTP {Host}:{Port}...",
                            _mailSettings.SmtpHost, _mailSettings.SmtpPort);

                        await smtpClient.SendMailAsync(message);

                        _logger.LogInformation("Email con factura HTML enviado exitosamente a {Email} para orden {BuyId}",
                            recipientEmail, buy.BuyId);
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

        public async Task SendContactEmailAsync(ContactForm contactForm)
        {
            _logger.LogInformation("Iniciando envío de email de contacto desde {Email}", contactForm.Email);

            try
            {
                // Validar parámetros de entrada
                if (contactForm == null)
                {
                    _logger.LogError("El formulario de contacto es null");
                    throw new ArgumentNullException(nameof(contactForm), "El formulario de contacto no puede ser null");
                }

                if (string.IsNullOrWhiteSpace(contactForm.Email) || string.IsNullOrWhiteSpace(contactForm.Name) || string.IsNullOrWhiteSpace(contactForm.Message))
                {
                    _logger.LogError("Datos del formulario incompletos");
                    throw new ArgumentException("El formulario debe tener nombre, email y mensaje completos");
                }

                // Validar configuración de email
                if (string.IsNullOrEmpty(_mailSettings.SmtpHost) || string.IsNullOrEmpty(_mailSettings.Username))
                {
                    _logger.LogError("Configuración de email incompleta. SmtpHost: {Host}, Username: {User}",
                        _mailSettings.SmtpHost ?? "(null)", _mailSettings.Username ?? "(null)");
                    throw new Exception("La configuración de email no está completa en appsettings.json");
                }

                _logger.LogInformation("Configuración de email: Host={Host}, Port={Port}",
                    _mailSettings.SmtpHost, _mailSettings.SmtpPort);

                // Crear el cuerpo del email en HTML
                string htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #1a365d 0%, #2563eb 100%); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8f9fa; padding: 30px; border-radius: 0 0 8px 8px; }}
        .field {{ margin-bottom: 20px; }}
        .label {{ font-weight: bold; color: #1a365d; margin-bottom: 5px; }}
        .value {{ background: white; padding: 10px; border-radius: 4px; border-left: 3px solid #2563eb; }}
        .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; text-align: center; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin: 0;'>📧 Nuevo Mensaje de Contacto</h2>
            <p style='margin: 5px 0 0 0; opacity: 0.9;'>InnovaTech - Formulario de Contacto</p>
        </div>
        <div class='content'>
            <div class='field'>
                <div class='label'>👤 Nombre:</div>
                <div class='value'>{contactForm.Name}</div>
            </div>
            <div class='field'>
                <div class='label'>📧 Correo Electrónico:</div>
                <div class='value'>{contactForm.Email}</div>
            </div>
            <div class='field'>
                <div class='label'>📱 Teléfono:</div>
                <div class='value'>{contactForm.Phone ?? "No proporcionado"}</div>
            </div>
            <div class='field'>
                <div class='label'>💬 Mensaje:</div>
                <div class='value'>{contactForm.Message}</div>
            </div>
            <div class='footer'>
                <p>Este mensaje fue enviado desde el formulario de contacto de InnovaTech</p>
                <p>Puedes responder directamente a {contactForm.Email}</p>
            </div>
        </div>
    </div>
</body>
</html>";

                // Configurar el mensaje
                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_mailSettings.SenderEmail, _mailSettings.SenderName);
                    message.To.Add(new MailAddress("pablodavidgomezv@gmail.com", "InnovaTech Soporte"));
                    message.ReplyToList.Add(new MailAddress(contactForm.Email, contactForm.Name)); // Para poder responder fácilmente
                    message.Subject = $"Nuevo mensaje de contacto - {contactForm.Name}";
                    message.Body = htmlBody;
                    message.IsBodyHtml = true;

                    _logger.LogDebug("Mensaje de email configurado. De: {From}, Para: pablodavidgomezv@gmail.com, Asunto: {Subject}",
                        message.From.Address, message.Subject);

                    // Configurar el cliente SMTP
                    using (var smtpClient = new SmtpClient(_mailSettings.SmtpHost, _mailSettings.SmtpPort))
                    {
                        smtpClient.Credentials = new NetworkCredential(_mailSettings.Username, _mailSettings.Password);
                        smtpClient.EnableSsl = true;
                        smtpClient.Timeout = 30000; // 30 segundos timeout

                        _logger.LogInformation("Enviando email de contacto a través de SMTP {Host}:{Port}...",
                            _mailSettings.SmtpHost, _mailSettings.SmtpPort);

                        await smtpClient.SendMailAsync(message);

                        _logger.LogInformation("Email de contacto enviado exitosamente desde {Email}",
                            contactForm.Email);
                    }
                }
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "Error SMTP al enviar email de contacto. StatusCode: {StatusCode}",
                    smtpEx.StatusCode);
                throw new Exception($"Error SMTP al enviar el email: {smtpEx.Message}. StatusCode: {smtpEx.StatusCode}", smtpEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar el email de contacto desde {Email}",
                    contactForm.Email);
                throw new Exception($"Error al enviar el email de contacto: {ex.Message}", ex);
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