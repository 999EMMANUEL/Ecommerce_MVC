using System;

namespace ProyectoEcommerce.Models
{
    public class BuyItem
    {
        public int BuyId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }

        public virtual Buy Buy { get; set; }
        public virtual Product Product { get; set; }
    }
}