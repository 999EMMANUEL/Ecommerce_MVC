using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Models
{
    public class ContactForm
    {

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Name { get; set; }

        [Required, EmailAddress(ErrorMessage = "Correo inválido")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Número no válido")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "El mensaje es obligatorio")]
        public string Message { get; set; }




    }
}
