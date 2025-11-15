using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Models
{
    public class Promotion
    {
        public int PromotionId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = default!;

        // Porcentaje de descuento (ej. 20 = 20%)
        public decimal DiscountPercent { get; set; } = 0m;

        // Texto corto para la etiqueta (ej. "20% OFF")
        [StringLength(50)]
        public string? BadgeText { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Productos asociados
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}