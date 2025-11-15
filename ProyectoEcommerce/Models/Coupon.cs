using System;
using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Models
{
    public class Coupon
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; }  // Ejemplo: NAVIDAD10

        [StringLength(200)]
        public string? Description { get; set; }

        // Descuento en porcentaje (1 a 100)
        [Range(1, 100, ErrorMessage = "El porcentaje debe estar entre 1 y 100")]
        public int DiscountPercent { get; set; }

        // Fecha de vigencia
        [DataType(DataType.Date)]
        public DateTime ValidFrom { get; set; }

        [DataType(DataType.Date)]
        public DateTime ValidTo { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
