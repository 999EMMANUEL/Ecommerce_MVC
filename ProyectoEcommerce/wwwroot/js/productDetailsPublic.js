// wwwroot/js/productDetailsPublic.js
// Funcionalidades para la vista pública de detalles de producto

class ProductDetailsPublic {
    constructor(productId, categoryId) {
        this.productId = productId;
        this.categoryId = categoryId;
        this.init();
    }

    init() {
        this.setupAddToCart();
        this.loadRelatedProducts();
    }

    setupAddToCart() {
        const addToCartBtn = document.querySelector('.add-to-cart');
        if (addToCartBtn) {
            addToCartBtn.addEventListener('click', () => {
                this.addToCart(this.productId);
            });
        }
    }

    addToCart(productId) {
        // Lógica para agregar al carrito
        console.log('Agregando producto al carrito:', productId);

        // Mostrar feedback al usuario
        const btn = document.querySelector('.add-to-cart');
        const originalText = btn.innerHTML;

        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Agregando...';
        btn.disabled = true;

        // Simular llamada a API (reemplazar con lógica real)
        setTimeout(() => {
            btn.innerHTML = '<i class="fas fa-check"></i> ¡Agregado!';
            btn.classList.remove('btn-primary');
            btn.classList.add('btn-success');

            // Mostrar notificación
            this.showNotification('Producto agregado al carrito', 'success');

            // Restaurar después de 2 segundos
            setTimeout(() => {
                btn.innerHTML = originalText;
                btn.disabled = false;
                btn.classList.remove('btn-success');
                btn.classList.add('btn-primary');
            }, 2000);
        }, 1000);
    }

    async loadRelatedProducts() {
        try {
            const response = await fetch(`/Products/GetRelatedProducts?categoryId=${this.categoryId}&currentProductId=${this.productId}`);

            if (!response.ok) {
                throw new Error('Error al cargar productos relacionados');
            }

            const products = await response.json();
            this.renderRelatedProducts(products);
        } catch (error) {
            console.error('Error cargando productos relacionados:', error);
            this.showRelatedProductsError();
        }
    }

    renderRelatedProducts(products) {
        const container = document.getElementById('relatedProducts');

        if (!products || products.length === 0) {
            container.innerHTML = '<p class="text-muted text-center">No hay productos relacionados disponibles</p>';
            return;
        }

        container.innerHTML = `
            <div class="row">
                ${products.map(product => `
                    <div class="col-md-3 mb-4">
                        <div class="card h-100 product-card">
                            <img src="${product.imageUrl ? product.imageUrl.replace('~/', '/') : '/images/no-image.png'}" 
     class="card-img-top" 
     alt="${product.name}"
     style="height: 200px; object-fit: cover;"
     onerror="this.src='/images/no-image.png'">
                            <div class="card-body d-flex flex-column">
                                <h6 class="card-title">${product.name}</h6>
                                <p class="card-text text-primary fw-bold mt-auto">₡${product.price.toFixed(2)}</p>
                                <a href="/Products/DetailsPublic/${product.id}" 
                                   class="btn btn-sm btn-outline-primary w-100 mt-2">
                                    Ver Producto
                                </a>
                            </div>
                        </div>
                    </div>
                `).join('')}
            </div>
        `;
    }

    showRelatedProductsError() {
        const container = document.getElementById('relatedProducts');
        container.innerHTML = `
            <div class="alert alert-warning text-center">
                <i class="fas fa-exclamation-triangle"></i>
                No se pudieron cargar los productos relacionados
            </div>
        `;
    }

    showNotification(message, type = 'info') {
        // Crear notificación toast
        const toast = document.createElement('div');
        toast.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
        toast.style.cssText = `
            top: 20px; 
            right: 20px; 
            z-index: 1050; 
            min-width: 300px;
        `;
        toast.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        document.body.appendChild(toast);

        // Auto-remover después de 5 segundos
        setTimeout(() => {
            if (toast.parentNode) {
                toast.remove();
            }
        }, 5000);
    }
}

// Inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', function () {
    // Obtener IDs del data attributes o de variables globales
    const productId = document.querySelector('[data-product-id]')?.getAttribute('data-product-id');
    const categoryId = document.querySelector('[data-category-id]')?.getAttribute('data-category-id');

    if (productId && categoryId) {
        window.productDetails = new ProductDetailsPublic(productId, categoryId);
    }
});