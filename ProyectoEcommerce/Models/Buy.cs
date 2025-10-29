using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Models
{
    public class Buy
    {
        public int BuyId { get; set; }

        [Required(ErrorMessage = "El ID del Cliente es obligatorio")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "El ID del empleado es obligatorio")]
        public int EmployeeId { get; set; }

        [DataType(DataType.Date)]
        public DateTime Fecha { get; set; }

        public decimal Subtotal { get; set; }
        public decimal IVA { get; set; }
        public decimal Total { get; set; }

        // Propiedades de navegación
        public virtual Customer Customer { get; set; }
        public virtual Employee Employee { get; set; }

        // Relación con productos a través de tabla intermedia
        public ICollection<BuyItem> Items { get; set; } = new List<BuyItem>();
      
    }
}