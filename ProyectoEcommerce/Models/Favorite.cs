using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ProyectoEcommerce.Models
{
    public class Favorite
    {
        public int FavoriteId { get; set; }

        // Usuario autenticado (nullable para soportar anónimos)
        public string? UserId { get; set; }

        // Identificador anónimo guardado en cookie (nullable)
        public string? AnonymousId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navegaciones 
        public virtual IdentityUser? User { get; set; }
        public virtual Product? Product { get; set; }
    }
}