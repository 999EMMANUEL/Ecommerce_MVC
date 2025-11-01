// wwwroot/js/categoriesMenu.js
// Cargar categoruas dinamicamente para el menu de navegacióo

class CategoriesMenu {
    constructor() {
        this.menuContainer = document.getElementById('categoriesMenu');
        this.init();
    }

    init() {
        this.loadCategories();
    }

    async loadCategories() {
        try {
            const response = await fetch('/Categories/GetCategoriesForMenu');

            if (!response.ok) {
                throw new Error('Error al cargar categorías');
            }

            const categories = await response.json();
            this.renderCategories(categories);
        } catch (error) {
            console.error('Error cargando categorías:', error);
            this.showError();
        }
    }

    renderCategories(categories) {
        if (!categories || categories.length === 0) {
            this.showNoCategories();
            return;
        }

        this.menuContainer.innerHTML = '';

        // Agrega cada categoria al menu
        categories.forEach(category => {
            const li = document.createElement('li');
            li.innerHTML = `
                <a class="dropdown-item" 
                   href="/Products/Public?categoryId=${category.categoryId}">
                    ${category.name}
                </a>
            `;
            this.menuContainer.appendChild(li);
        });

        // Agrega separador y link para ver todas
        //this.addViewAllLink();
    }

   // addViewAllLink() {
     //   const divider = document.createElement('li');
       // divider.innerHTML = `<hr class="dropdown-divider">`;
      //  this.menuContainer.appendChild(divider);

       // const viewAll = document.createElement('li');
       // viewAll.innerHTML = `
        //    <a class="dropdown-item text-primary fw-bold" 
         //      href="/Categories/Public">
         //       <i class="fas fa-folder-open"></i> Ver Todas las Categorías
        //`;
       // this.menuContainer.appendChild(viewAll);
   // }

    showNoCategories() {
        this.menuContainer.innerHTML = `
            <li><a class="dropdown-item text-muted" href="/Categories/Public">No hay categorías disponibles</a></li>
        `;
    }

    showError() {
        this.menuContainer.innerHTML = `
            <li><a class="dropdown-item text-danger" href="/Categories/Public">Error cargando categorías</a></li>
        `;
    }

    // Metodo para recargar categorías ( despues de crear/editar)
    refresh() {
        this.loadCategories();
    }
}

// Inicializa cuando el DOM este listo
document.addEventListener('DOMContentLoaded', function () {
    window.categoriesMenu = new CategoriesMenu();
});