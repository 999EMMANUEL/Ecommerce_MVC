using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Models
{
    public class Product
    {

        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required(ErrorMessage = "El precio de unidad es obligatorio")]
        public decimal Price { get; set; }

        public bool Available { get; set; }

        public string ImageUrl { get; set; }
        [Required(ErrorMessage = "El stock es obligatorio")]
        public int Stock { get; set; }
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }

        // Relaciones
        public ICollection<ShoppingCartItem> ShoppingCartItems { get; set; } = new List<ShoppingCartItem>();
        public ICollection<BuyItem> BuyItems { get; set; } = new List<BuyItem>();
    }
}
