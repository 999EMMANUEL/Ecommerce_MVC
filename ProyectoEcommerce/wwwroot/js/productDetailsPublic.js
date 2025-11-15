// Funcionalidades para la vista pública de detalles de producto
// (archivo actualizado: se añadió renderizado de badge de promoción y el handler AJAX que usa /ShoppingCarts/AddAjax)

class ProductDetailsPublic {
    constructor(productId, categoryId) {
        this.productId = productId;
        this.categoryId = categoryId;
        this.init();
    }

    init() {
        this.setupAddToCart();
        this.loadRelatedProducts();
        this.setupFavorites();
        this.checkFavoriteStatus();
        this.loadFavoritesCount();
    }

    setupAddToCart() {
        const addToCartForms = document.querySelectorAll('.add-to-cart-form');

        addToCartForms.forEach(form => {
            form.addEventListener('submit', function (e) {
                const button = this.querySelector('.add-to-cart-btn');
                const buttonText = button.querySelector('.button-text');
                const spinner = button.querySelector('.spinner-border');

                // Mostrar loading inmediatamente - el servidor se encargará del resto
                button.disabled = true;
                buttonText.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Procesando...';
                spinner.classList.remove('d-none');

                // Permitir que el formulario se envíe normalmente
                // El servidor redirigirá al login si es necesario
            });
        });
    }

    addToCart(productId) {
        //  agregar al carrito
        console.log('Agregando producto al carrito:', productId);

        // Mostrar feedback al usuario
        const btn = document.querySelector('.add-to-cart');
        const originalText = btn.innerHTML;

        btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Agregando...';
        btn.disabled = true;

        // 
        setTimeout(() => {
            btn.innerHTML = '<i class="fas fa-check"></i> ¡Agregado!';
            btn.classList.remove('btn-primary');
            btn.classList.add('btn-success');

            // Mostra notificación
            this.showNotification('Producto agregado al carrito', 'success');

            // Restaura después de 1 segundo
            setTimeout(() => {
                btn.innerHTML = originalText;
                btn.disabled = false;
                btn.classList.remove('btn-success');
                btn.classList.add('btn-primary');
            }, 1000);
        }, 500);
    }

    // ========== MÉTODOS PARA FAVORITOS ==========

    setupFavorites() {
        const favoriteBtn = document.querySelector('.favorite-btn');
        if (favoriteBtn) {
            favoriteBtn.addEventListener('click', () => {
                const productId = favoriteBtn.getAttribute('data-product-id');
                this.toggleFavorite(productId);
            });
        }
    }

    async checkFavoriteStatus() {
        try {
            const response = await fetch(`/Favorites/CheckFavorite?productId=${this.productId}`);
            const result = await response.json();

            if (result.success) {
                this.updateFavoriteUI(result.isFavorite);
            }
        } catch (error) {
            console.error('Error verificando favorito:', error);
        }
    }

    async toggleFavorite(productId) {
        try {
            const response = await fetch('/Favorites/ToggleFavorite', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(productId)
            });

            const result = await response.json();

            if (result.success) {
                // Actualiza icono del botón
                this.updateFavoriteButton(result.isFavorite);

                // Mostrar notificación
                this.showNotification(result.message, result.isFavorite ? 'success' : 'info');

                // Actualiza contador en header
                this.loadFavoritesCount();
            } else {
                this.showNotification('Error: ' + result.message, 'error');
            }

        } catch (error) {
            console.error('Error en toggleFavorite:', error);
            this.showNotification('Error al actualizar favoritos', 'error');
        }
    }

    updateFavoriteButton(isFavorite) {
        const favoriteBtn = document.querySelector('.favorite-btn');
        const icon = favoriteBtn.querySelector('i');

        if (isFavorite) {
            icon.className = 'fas fa-heart';
            favoriteBtn.classList.remove('btn-outline-secondary');
            favoriteBtn.classList.add('btn-danger', 'text-white'); // Texto blanco 
            favoriteBtn.innerHTML = '<i class="fas fa-heart"></i> Favorito';
            favoriteBtn.title = "Remover de favoritos";
        } else {
            icon.className = 'far fa-heart';
            favoriteBtn.classList.remove('btn-danger', 'text-white');
            favoriteBtn.classList.add('btn-outline-secondary');
            favoriteBtn.innerHTML = '<i class="far fa-heart"></i> Favorito';
            favoriteBtn.title = "Agregar a favoritos";
        }
    }

    updateFavoriteUI(isFavorite) {
        const favoriteBtn = document.querySelector('.favorite-btn');
        if (!favoriteBtn) return;

        const favoriteIcon = favoriteBtn.querySelector('i');

        if (isFavorite) {
            favoriteIcon.className = 'fas fa-heart'; // Corazón lleno
            favoriteBtn.classList.add('text-danger');
            favoriteBtn.title = "Remover de favoritos";
        } else {
            favoriteIcon.className = 'far fa-heart'; // Corazón vacío
            favoriteBtn.classList.remove('text-danger');
            favoriteBtn.title = "Agregar a favoritos";
        }
    }

    async loadFavoritesCount() {
        try {
            const response = await fetch('/Favorites/GetFavorites');

            if (!response.ok) {
                throw new Error(`Error HTTP: ${response.status}`);
            }

            const data = await response.json();

            if (data.success) {
                this.updateFavoritesBadge(data.favorites.length);
            } else {
                this.updateFavoritesBadge(0);
            }

        } catch (error) {
            console.error('Error cargando contador de favoritos:', error);
            this.updateFavoritesBadge(0);
        }
    }

    updateFavoritesCounter() {
        // Incrementa el contador visualmente
        const favoritesBadge = document.querySelector('.favorites-badge');
        if (favoritesBadge) {
            const currentCount = parseInt(favoritesBadge.textContent) || 0;
            const newCount = currentCount + 1;
            favoritesBadge.textContent = newCount;
            favoritesBadge.style.display = newCount > 0 ? 'block' : 'none';
        }

        // También recarga el conteo real del servidor
        this.loadFavoritesCount();
    }

    updateFavoritesBadge(count) {
        const favoritesBadge = document.querySelector('.favorites-badge');
        if (favoritesBadge) {
            if (count > 0) {
                favoritesBadge.textContent = count;
                favoritesBadge.style.display = 'block';
            } else {
                favoritesBadge.style.display = 'none';
            }
        }
    }

    // ========== MÉTODOS  ==========

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
                            <div class="position-relative" style="height:200px; overflow:hidden;">
                                <img src="${product.imageUrl ? product.imageUrl.replace('~/', '/') : '/images/no-image.png'}" 
                                     class="card-img-top w-100 h-100" 
                                     alt="${product.name}"
                                     style="object-fit:cover; display:block;"
                                     onerror="this.src='/images/no-image.png'">
                                ${product.promotion ? `
                                    <div class="badge bg-danger position-absolute" 
                                         style="top:10px; left:10px; z-index:20; font-weight:700; padding:0.45rem 0.6rem;">
                                        ${product.promotion.badgeText ?? (product.promotion.discountPercent ? product.promotion.discountPercent + '% OFF' : 'Oferta')}
                                    </div>
                                    <div class="position-absolute text-white small" style="bottom:8px; left:10px; z-index:20; background:rgba(0,0,0,0.45); padding:0.25rem 0.4rem; border-radius:4px;">
                                        Hasta ${new Date(product.promotion.endsAt).toLocaleDateString()}
                                    </div>
                                ` : ''}
                            </div>
                            <div class="card-body d-flex flex-column">
                                <h6 class="card-title">${product.name}</h6>
                                <p class="card-text text-primary fw-bold mt-auto">₡${product.price.toLocaleString('en-US')}</p>
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
        // Crear notificación toast...Agregado a favoritos/Removido de favoritos
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

        // Auto-remover después de...900
        setTimeout(() => {
            if (toast.parentNode) {
                toast.remove();
            }
        }, 900);
    }
}

// Inicializar cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', function () {
  
    const productId = document.querySelector('[data-product-id]')?.getAttribute('data-product-id');
    const categoryId = document.querySelector('[data-category-id]')?.getAttribute('data-category-id');

    if (productId && categoryId) {
        window.productDetails = new ProductDetailsPublic(productId, categoryId);
    }
});



// Spinner para agregar al carrito con delay
document.addEventListener('DOMContentLoaded', function () {
    const addToCartForms = document.querySelectorAll('.add-to-cart-form');

    addToCartForms.forEach(form => {
        form.addEventListener('submit', async function (e) {
            e.preventDefault(); // Prevenir envío inmediato

            const button = this.querySelector('.add-to-cart-btn');
            const buttonText = button.querySelector('.button-text');
            const spinner = button.querySelector('.spinner-border');

            // Mostrar spinner y deshabilitar botón
            button.disabled = true;
            buttonText.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Agregando...';
            spinner.classList.remove('d-none');

            try {
                // Delay artificial de 1.5 segundos
                await new Promise(resolve => setTimeout(resolve, 1500));

                // Crear un formulario temporal para enviar los datos
                const formData = new FormData(this);
                const response = await fetch(this.action, {
                    method: 'POST',
                    body: formData
                });

                if (response.ok) {
                    // Éxito - mostrar confirmación
                    buttonText.innerHTML = '<i class="fas fa-check me-1"></i>¡Agregado!';
                    spinner.classList.add('d-none');

                    // Recargar después de 1 segundo más
                    setTimeout(() => {
                        window.location.reload();
                    }, 1000);

                } else {
                    throw new Error('Error al agregar al carrito');
                }

            } catch (error) {
                console.error('Error:', error);
                buttonText.innerHTML = '<i class="fas fa-exclamation-triangle me-1"></i>Error';
                spinner.classList.add('d-none');

                // Restaurar después de 2 segundos
                setTimeout(() => {
                    resetAddToCartButton(button);
                }, 2000);
            }
        });
    });

    function resetAddToCartButton(button) {
        const buttonText = button.querySelector('.button-text');
        const spinner = button.querySelector('.spinner-border');

        button.disabled = false;
        buttonText.innerHTML = '<i class="fas fa-cart-plus me-1"></i>Agregar al Carrito';
        spinner.classList.add('d-none');
    }

    
});


                // JavaScript para botones "Agregar al Carrito" en favoritos
document.addEventListener('DOMContentLoaded', function () {
    const addToCartButtons = document.querySelectorAll('.add-to-cart');

    addToCartButtons.forEach(button => {
        button.addEventListener('click', async function (e) {
            e.preventDefault();

            const productId = this.getAttribute('data-product-id');
            const buttonText = this.querySelector('.button-text');
            const spinner = this.querySelector('.spinner-border');

            // Verificar que existan los elementos
            if (!buttonText || !spinner) {
                console.error('No se encontraron los elementos del spinner');
                return;
            }

            // Mostrar spinner y deshabilitar botón
            this.disabled = true;
            buttonText.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Agregando...';
            spinner.classList.remove('d-none');

            try {
                await new Promise(resolve => setTimeout(resolve, 600));

                // Obtener token antiforgery: intenta input oculto, luego meta
                const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value
                    || document.querySelector('meta[name="RequestVerificationToken"]')?.getAttribute('content') || '';

                const response = await fetch('/ShoppingCarts/AddAjax', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': token
                    },
                    body: JSON.stringify({ productId: parseInt(productId, 10), quantity: 1 })
                });

                if (!response.ok) throw new Error('Respuesta no OK del servidor');

                const result = await response.json();
                if (result.success) {
                    buttonText.innerHTML = '<i class="fas fa-check me-1"></i>¡Agregado!';
                    spinner.classList.add('d-none');
                    setTimeout(() => window.location.reload(), 900);
                } else {
                    throw new Error(result.message || 'Error al agregar al carrito');
                }
            } catch (error) {
                console.error('Error:', error);
                buttonText.innerHTML = '<i class="fas fa-exclamation-triangle me-1"></i>Error';
                spinner.classList.add('d-none');
                
                setTimeout(() => {
                    this.disabled = false;
                    buttonText.innerHTML = '<i class="fas fa-cart-plus me-1"></i>Agregar al Carrito';
                }, 2000);
            }
        });
    });
});