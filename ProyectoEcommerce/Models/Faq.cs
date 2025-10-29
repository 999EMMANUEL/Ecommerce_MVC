using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Models
{
    public class Faq
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string Category { get; set; } = "General";

        [Required, StringLength(200)]
        public string Question { get; set; } = default!;

        [Required]
        public string Answer { get; set; } = default!;

        public int SortOrder { get; set; } = 0;   // para ordenar dentro de la categoría
        public bool IsActive { get; set; } = true;
    }
}

