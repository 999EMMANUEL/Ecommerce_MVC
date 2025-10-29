namespace ProyectoEcommerce.Areas.Admin.ViewModels
{
    public class AdminDashboardVM
    {
        public int TotalUsuarios { get; set; }
        public int PedidosHoy { get; set; }
        public decimal VentasHoy { get; set; }
        public int ProductosActivos { get; set; }
    }
}
