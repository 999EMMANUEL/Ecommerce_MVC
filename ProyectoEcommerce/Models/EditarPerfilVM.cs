using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Models
{
    public class EditarPerfilVM
    {
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "El nombre completo es requerido")]
        [Display(Name = "Nombre Completo")]
        public string Name_full { get; set; }

        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [Display(Name = "Dirección")]
        public string Direccion { get; set; }
    }
}
