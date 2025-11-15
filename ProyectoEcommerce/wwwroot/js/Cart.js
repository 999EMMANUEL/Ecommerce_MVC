// Sistema de Carrito de Compras
class ShoppingCart {
    constructor() {
        this.cartItems = this.loadCartFromStorage();
        // Delay para asegurar que el DOM esté completamente cargado
        setTimeout(() => {
            this.updateCartUI();
            this.initializeEventListeners();
        }, 150);
    }

    // Cargar carrito desde localStorage
    loadCartFromStorage() {
        try {
            const savedCart = localStorage.getItem('innovatech_cart');
            return savedCart ? JSON.parse(savedCart) : [];
        } catch (error) {
            console.error('Error loading cart from storage:', error);
            return [];
        }
    }

    // Guardar carrito en localStorage
    saveCartToStorage() {
        try {
            localStorage.setItem('innovatech_cart', JSON.stringify(this.cartItems));
        } catch (error) {
            console.error('Error saving cart to storage:', error);
        }
    }

    // Inicializar event listeners
    initializeEventListeners() {
        // Delay pequeño para asegurar que los elementos estén en el DOM
        setTimeout(() => {
            // Delegación de eventos para botones de remover
            document.addEventListener('click', (e) => {
                if (e.target.closest('.remove-from-cart')) {
                    const button = e.target.closest('.remove-from-cart');
                    const productId = button.getAttribute('data-product-id');

                    // Pequeño delay para feedback táctil
                    setTimeout(() => {
                        this.removeFromCart(productId);
                    }, 100);
                }

                if (e.target.closest('.update-quantity')) {
                    const button = e.target.closest('.update-quantity');
                    const productId = button.getAttribute('data-product-id');
                    const change = parseInt(button.getAttribute('data-change'));

                    // Delay para feedback visual
                    setTimeout(() => {
                        this.updateQuantity(productId, change);
                    }, 50);
                }
            });
        }, 200);
    }

    // Agregar producto al carrito
    async addToCart(productId, productName, productPrice, productImage, quantity = 1) {
        try {
            // Buscar si el producto ya está en el carrito
            const existingItemIndex = this.cartItems.findIndex(item => item.productId === productId);

            if (existingItemIndex > -1) {
                // Actualizar cantidad si ya existe
                this.cartItems[existingItemIndex].quantity += quantity;
            } else {
                // Agregar nuevo item
                this.cartItems.push({
                    productId: productId,
                    name: productName,
                    price: productPrice,
                    image: productImage,
                    quantity: quantity,
                    addedAt: new Date().toISOString()
                });
            }

            // Guardar y actualizar UI
            this.saveCartToStorage();

            // Delay para suavizar la actualización de la UI
            setTimeout(() => {
                this.updateCartUI();
            }, 100);

            // Delay para la notificación (mejor timing visual)
            setTimeout(() => {
                this.showNotification('✅ Producto agregado al carrito', 'success');
            }, 300);

            return true;

        } catch (error) {
            console.error('Error adding to cart:', error);

            // Delay para notificación de error
            setTimeout(() => {
                this.showNotification('❌ Error al agregar al carrito', 'error');
            }, 300);

            return false;
        }
    }

    // Remover producto del carrito
    removeFromCart(productId) {
        this.cartItems = this.cartItems.filter(item => item.productId !== productId);
        this.saveCartToStorage();

        // Delay para suavizar la animación de remoción
        setTimeout(() => {
            this.updateCartUI();
        }, 150);

        // Delay para notificación de eliminación
        setTimeout(() => {
            this.showNotification('🗑️ Producto removido del carrito', 'info');
        }, 250);
    }

    // Actualizar cantidad
    updateQuantity(productId, change) {
        const item = this.cartItems.find(item => item.productId === productId);
        if (item) {
            const newQuantity = item.quantity + change;
            if (newQuantity <= 0) {
                this.removeFromCart(productId);
                return;
            }
            item.quantity = newQuantity;
            this.saveCartToStorage();

            // Pequeño delay para suavizar la actualización de cantidad
            setTimeout(() => {
                this.updateCartUI();
            }, 80);
        }
    }

    // Actualizar la interfaz de usuario
    updateCartUI() {
        this.updateCartCounter();
        this.updateCartDropdown();
    }

    // Actualizar contador del badge
    updateCartCounter() {
        const badge = document.querySelector('.cart-badge');
        if (badge) {
            const totalItems = this.getTotalItems();
            badge.textContent = totalItems;
            badge.style.display = totalItems > 0 ? 'block' : 'none';
        }
    }

    // Actualizar dropdown del carrito
    updateCartDropdown() {
        const cartItemsList = document.getElementById('cartItemsList');
        const emptyMessage = document.getElementById('emptyCartMessage');
        const cartTotal = document.getElementById('cartTotal');

        if (!cartItemsList || !emptyMessage) return;

        if (this.cartItems.length === 0) {
            cartItemsList.innerHTML = '';
            emptyMessage.style.display = 'block';
            if (cartTotal) cartTotal.textContent = '₡0';
            return;
        }

        emptyMessage.style.display = 'none';

        cartItemsList.innerHTML = this.cartItems.map(item => `
            <div class="dropdown-item">
                <div class="d-flex align-items-center gap-2">
                    <img src="${item.image || '/images/no-image.png'}" 
                         alt="${item.name}" 
                         class="rounded" 
                         style="width: 50px; height: 50px; object-fit: cover;">
                    <div class="flex-grow-1">
                        <div class="fw-semibold small text-truncate">${item.name}</div>
                        <div class="text-primary fw-bold">₡${(item.price * item.quantity).toLocaleString()}</div>
                        <div class="d-flex align-items-center gap-2 mt-1">
                            <button class="btn btn-sm btn-outline-secondary update-quantity" 
                                    data-product-id="${item.productId}" 
                                    data-change="-1">
                                <i class="fas fa-minus"></i>
                            </button>
                            <span class="quantity-badge badge bg-secondary">${item.quantity}</span>
                            <button class="btn btn-sm btn-outline-secondary update-quantity" 
                                    data-product-id="${item.productId}" 
                                    data-change="1">
                                <i class="fas fa-plus"></i>
                            </button>
                        </div>
                    </div>
                    <button class="btn btn-sm btn-outline-danger remove-from-cart" 
                            data-product-id="${item.productId}"
                            title="Eliminar">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            </div>
        `).join('');

        // Actualizar total
        if (cartTotal) {
            cartTotal.textContent = `₡${this.getTotalPrice().toLocaleString()}`;
        }
    }

    // Obtener total de items
    getTotalItems() {
        return this.cartItems.reduce((total, item) => total + item.quantity, 0);
    }

    // Obtener total precio
    getTotalPrice() {
        return this.cartItems.reduce((total, item) => total + (item.price * item.quantity), 0);
    }

    // Mostrar notificación
    showNotification(message, type = 'info') {
        const alert = document.createElement('div');
        alert.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
        alert.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        alert.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        document.body.appendChild(alert);

        // Auto-remover después de 3 segundos
        setTimeout(() => {
            if (alert.parentElement) {
                alert.remove();
            }
        }, 3000);
    }

    // Limpiar carrito
    clearCart() {
        this.cartItems = [];
        this.saveCartToStorage();

        // Delay para suavizar la limpieza
        setTimeout(() => {
            this.updateCartUI();
        }, 200);
    }
}

// Inicializar carrito cuando se carga la página
document.addEventListener('DOMContentLoaded', function () {
    // Delay para asegurar que todo esté completamente cargado
    setTimeout(() => {
        window.shoppingCart = new ShoppingCart();
    }, 300);

    // Agregar eventos a los botones "Agregar al Carrito"
    document.addEventListener('click', function (e) {
        if (e.target.closest('.add-to-cart')) {
            const button = e.target.closest('.add-to-cart');
            const productId = button.getAttribute('data-product-id');
            const productName = button.getAttribute('data-product-name') || 'Producto';
            const productPrice = parseFloat(button.getAttribute('data-product-price') || 0);
            const productImage = button.getAttribute('data-product-image') || '/images/no-image.png';

            // Mostrar loading inmediatamente
            const originalText = button.innerHTML;
            button.innerHTML = '<i class="fas fa-spinner fa-spin"></i>';
            button.disabled = true;

            // Delay antes de procesar (para mejor experiencia visual)
            setTimeout(async () => {
                try {
                    // Agregar al carrito
                    await window.shoppingCart.addToCart(productId, productName, productPrice, productImage, 1);
                } catch (error) {
                    console.error('Error:', error);
                } finally {
                    // Delay para restaurar el botón (que se vea el spinner)
                    setTimeout(() => {
                        button.innerHTML = originalText;
                        button.disabled = false;
                    }, 600);
                }
            }, 200);

            e.preventDefault();
        }
    });
});