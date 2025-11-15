namespace ProyectoEcommerce.Models
{
    public class ProductFavoriteViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }
        public bool Available { get; set; }
        public int Stock { get; set; }
    }
}