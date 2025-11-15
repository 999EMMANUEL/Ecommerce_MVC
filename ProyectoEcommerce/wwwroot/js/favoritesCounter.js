// favoritesCounter.js - Sistema global de contador de favoritos
class FavoritesCounter {
    constructor() {
        this.badge = document.querySelector('.favorites-badge');
        this.isInitialized = false;
    }

    async init() {
        if (this.isInitialized) return;

        await this.updateCounter();

        // Escuchar eventos de actualización
        document.addEventListener('favoritesUpdated', () => {
            this.updateCounter();
        });

        // Actualizar periódicamente por si hay cambios en otras pestañas
        setInterval(() => {
            this.updateCounter();
        }, 30000);

        this.isInitialized = true;
    }

    async updateCounter() {
        try {
            const response = await fetch('/Favorites/GetFavoritesCount');

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();
            this.updateBadge(data.count);
        } catch (error) {
            console.warn('Error actualizando contador de favoritos:', error);
            this.updateBadge(0);
        }
    }

    updateBadge(count) {
        if (!this.badge) return;

        if (count > 0) {
            this.badge.textContent = count;
            this.badge.style.display = 'block';

            // Animación sutil
            this.badge.style.transform = 'scale(1.3)';
            setTimeout(() => {
                if (this.badge) {
                    this.badge.style.transform = 'scale(1)';
                }
            }, 200);
        } else {
            this.badge.style.display = 'none';
        }
    }

    // Método público para forzar actualización
    async refresh() {
        await this.updateCounter();
    }
}

// Inicialización global
document.addEventListener('DOMContentLoaded', function () {
    window.favoritesCounter = new FavoritesCounter();
    window.favoritesCounter.init();
});

// Función global para uso desde cualquier página
window.updateFavoritesCounter = function () {
    if (window.favoritesCounter) {
        window.favoritesCounter.refresh();
    }
};