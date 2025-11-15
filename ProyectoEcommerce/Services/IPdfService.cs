using ProyectoEcommerce.Models;
using System.Threading.Tasks;

namespace ProyectoEcommerce.Services
{
    public interface IPdfService
    {
        Task<byte[]> GenerateInvoicePdfAsync(Buy buy);
    }
}