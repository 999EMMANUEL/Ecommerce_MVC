using System;
using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Models
{
    public class Review
    {
        public int ReviewId { get; set; }

        // Producto reseñado
        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        // Usuario que deja la reseña
        public string UserId { get; set; } = default!;

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required, StringLength(1000)]
        public string Comment { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
