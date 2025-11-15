using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProyectoEcommerce.Models;
using ProyectoEcommerce.Services;
using System;
using System.Threading.Tasks;

namespace ProyectoEcommerce.Controllers
{
    public class ContactController : Controller
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<ContactController> _logger;
        private readonly IAntiforgery _antiforgery;

        public ContactController(IEmailService emailService, ILogger<ContactController> logger, IAntiforgery antiforgery)
        {
            _emailService = emailService;
            _logger = logger;
            _antiforgery = antiforgery;
        }

        // GET: Contact
        public ActionResult Index()
        {
            return View();
        }

        // POST: Contact/SendMessage
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ContactForm contactForm)
        {
            _logger.LogInformation("Recibiendo mensaje de contacto desde {Email}", contactForm?.Email);

            try
            {
                // Validar token antiforgery manualmente
                try
                {
                    await _antiforgery.ValidateRequestAsync(HttpContext);
                }
                catch (AntiforgeryValidationException)
                {
                    _logger.LogWarning("Token antiforgery inválido");
                    return BadRequest(new { success = false, message = "Token de seguridad inválido" });
                }

                if (contactForm == null)
                {
                    _logger.LogWarning("Formulario de contacto vacío recibido");
                    return BadRequest(new { success = false, message = "Datos del formulario no válidos" });
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Modelo de contacto inválido");
                    return BadRequest(new { success = false, message = "Por favor completa todos los campos obligatorios" });
                }

                // Enviar el email
                await _emailService.SendContactEmailAsync(contactForm);

                _logger.LogInformation("Email de contacto procesado exitosamente desde {Email}", contactForm.Email);

                return Ok(new { success = true, message = "Mensaje enviado correctamente. Te contactaremos pronto." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar mensaje de contacto desde {Email}", contactForm?.Email);

                return StatusCode(500, new
                {
                    success = false,
                    message = "Error al enviar el mensaje. Por favor intenta nuevamente o contáctanos directamente."
                });
            }
        }
    }
}
