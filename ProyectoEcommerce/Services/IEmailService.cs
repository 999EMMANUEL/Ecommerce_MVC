using ProyectoEcommerce.Models;
using System.Threading.Tasks;

namespace ProyectoEcommerce.Services
{
    public interface IEmailService
    {
        Task SendInvoiceEmailAsync(Buy buy, string recipientEmail, string recipientName);
        Task SendContactEmailAsync(ContactForm contactForm);
    }
}