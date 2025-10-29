using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        public string Name { get; set; }
        public string Description { get; set; }
        public virtual ICollection<Product> Products { get; set; }


    }
}
