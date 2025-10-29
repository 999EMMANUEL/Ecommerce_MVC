using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Models
{
    public class Testimonial
    {
        public int TestimonialId { get; set; }

        [Required]
        public string Comment { get; set; }

        public DateTime CreatedDate { get; set; }
        public bool IsApproved { get; set; } // Para moderación

        public int CustomerId { get; set; }
        public virtual Customer Customer { get; set; }
    }
}