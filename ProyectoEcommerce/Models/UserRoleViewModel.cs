using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProyectoEcommerce.Models
{
    // ViewModel para mostrar usuarios con sus roles
    public class UserRoleViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; }

        [Display(Name = "Nombre de Usuario")]
        public string UserName { get; set; }

        [Display(Name = "Email Confirmado")]
        public bool EmailConfirmed { get; set; }

        [Display(Name = "Roles")]
        public List<string> Roles { get; set; } = new List<string>();
    }

    // ViewModel para gestionar roles de un usuario
    public class ManageUserRolesViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; }

        [Display(Name = "Nombre de Usuario")]
        public string UserName { get; set; }

        public List<RoleSelectionViewModel> Roles { get; set; } = new List<RoleSelectionViewModel>();
    }

    // ViewModel para selección de roles
    public class RoleSelectionViewModel
    {
        [Display(Name = "Rol")]
        public string RoleName { get; set; }

        [Display(Name = "Asignado")]
        public bool IsSelected { get; set; }
    }
}