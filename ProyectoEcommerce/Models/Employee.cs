using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; }

        [Required]
        public string Name { get; set; }

        public string position { get; set; }

        public string direccion { get; set; }

        [DataType(DataType.Date)]
        public DateTime FechaNacimiento { get; set; }
        [DataType(DataType.Date)]
        public DateTime Contratacion { get; set; }
        public virtual ICollection<Buy> BuysHandled { get; set; }

    }
}
