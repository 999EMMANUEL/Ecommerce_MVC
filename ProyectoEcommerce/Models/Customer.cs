using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }

        [Display(Name = "Nombre completo")]
        [StringLength(120)]
        public string Name_full { get; set; }

        [Display(Name = "Correo")]
        [EmailAddress]
        [Required]
        public string Email { get; set; } = default!;

        [Display(Name = "Teléfono")]
        [StringLength(40)]
        public string Telefono { get; set; }

        [Display(Name = "Dirección")]
        [StringLength(250)]
        public string Direccion { get; set; }
        public virtual ICollection<ShoppingCart> ShoppingCarts { get; set; }
        public virtual ICollection<Buy> Buys { get; set; }
    }
}
