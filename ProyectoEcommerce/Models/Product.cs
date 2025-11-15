using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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

        // contador de vistas
        public int ViewCount { get; set; } = 0;

        // filtra para no incluir productos creados en las últimas X horas
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

       
        public bool IsFeatured { get; set; } = false;

        // Relaciones
        public ICollection<ShoppingCartItem> ShoppingCartItems { get; set; } = new List<ShoppingCartItem>();
        public ICollection<BuyItem> BuyItems { get; set; } = new List<BuyItem>();

     
        public int? PromotionId { get; set; }
        public virtual Promotion? Promotion { get; set; }


        public ICollection<Review> Reviews { get; set; } = new List<Review>();




        // ----------------- Helpers de precio con promoción -----------------

        [NotMapped]
        public decimal EffectivePrice => CalculateEffectivePrice(DateTime.UtcNow);

        // Devuelve cuánto se descuenta (original - efectivo)
        [NotMapped]
        public decimal DiscountAmount => Math.Max(0m, Price - EffectivePrice);

        // Calcula el precio efectivo en una fecha dada (
        public decimal CalculateEffectivePrice(DateTime nowUtc)
        {
            if (Promotion == null) return Price;

            // Considerar sólo promociones activas y con porcentaje > 0
            if (!Promotion.IsActive) return Price;
            if (Promotion.DiscountPercent <= 0) return Price;

            // Usamos UTC para comparar
            var start = Promotion.StartDate;
            var end = Promotion.EndDate;

            if (start <= nowUtc && end >= nowUtc)
            {
                var factor = 1m - (Promotion.DiscountPercent / 100m);
                var discounted = Math.Round(Price * factor, 2, MidpointRounding.AwayFromZero);
                return discounted > 0 ? discounted : Price;
            }

            return Price;
        }
    }
}


