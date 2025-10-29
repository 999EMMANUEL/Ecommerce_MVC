// Accessibility.js - Control del panel de accesibilidad
class AccessibilityOptions {
    constructor() {
        this.init();
    }

    init() {
        console.log('Accessibility options initialized');
        this.loadSavedOptions();
        this.setupEventListeners();
        this.setupPanelToggle();
    }

    loadSavedOptions() {
        const fontSize = localStorage.getItem('accessibility-fontSize') || 'normal';
        const contrast = localStorage.getItem('accessibility-contrast') || 'normal';

        console.log('Loading saved options:', { fontSize, contrast });

        this.applyFontSize(fontSize);
        this.applyContrast(contrast);
        this.updateActiveButtons();
    }

    setupEventListeners() {
        console.log('Setting up event listeners');

        // Event delegation para los botones de accesibilidad
        document.addEventListener('click', (e) => {
            if (e.target.classList.contains('font-size-btn')) {
                e.preventDefault();
                const size = e.target.id.replace('font-', '');
                this.setFontSize(size);
            }
            else if (e.target.classList.contains('contrast-btn')) {
                e.preventDefault();
                const contrast = e.target.id.replace('contrast-', '');
                this.setContrast(contrast);
            }
            else if (e.target.id === 'reset-accessibility') {
                e.preventDefault();
                this.resetOptions();
            }
        });
    }

    setupPanelToggle() {
        const btn = document.getElementById('btnAccessibility');
        const panel = document.getElementById('accessibilityPanel');

        if (!btn || !panel) {
            console.error('No se encontraron los elementos del panel de accesibilidad');
            return;
        }

        const togglePanel = (e) => {
            e.preventDefault();
            e.stopPropagation();

            if (panel.classList.contains('is-open')) {
                panel.classList.remove('is-open');
                btn.setAttribute('aria-expanded', 'false');
            } else {
                panel.classList.add('is-open');
                btn.setAttribute('aria-expanded', 'true');
            }
        };

        // Cerrar panel al hacer clic fuera
        const closePanel = (e) => {
            if (panel.classList.contains('is-open') &&
                !panel.contains(e.target) &&
                e.target !== btn) {
                panel.classList.remove('is-open');
                btn.setAttribute('aria-expanded', 'false');
            }
        };

        // Cerrar con tecla Escape
        const closeOnEscape = (e) => {
            if (e.key === 'Escape' && panel.classList.contains('is-open')) {
                panel.classList.remove('is-open');
                btn.setAttribute('aria-expanded', 'false');
                btn.focus();
            }
        };

        btn.addEventListener('click', togglePanel);
        document.addEventListener('click', closePanel);
        document.addEventListener('keydown', closeOnEscape);
    }

    setFontSize(size) {
        console.log('Setting font size to:', size);
        localStorage.setItem('accessibility-fontSize', size);
        this.applyFontSize(size);
        this.updateActiveButtons();
    }

    setContrast(contrast) {
        console.log('Setting contrast to:', contrast);
        localStorage.setItem('accessibility-contrast', contrast);
        this.applyContrast(contrast);
        this.updateActiveButtons();
    }

    applyFontSize(size) {
        // Limpiar clases anteriores
        const body = document.body;
        body.classList.remove('font-small', 'font-normal', 'font-large', 'font-xlarge');

        // Aplicar nueva clase
        body.classList.add(`font-${size}`);

        console.log('Applied font size class:', `font-${size}`);
    }

    applyContrast(contrast) {
        console.log('Applying contrast:', contrast);

        // Remover todas las clases de contraste
        document.body.classList.remove('high-contrast-mode', 'inverted-contrast-mode');

        // Aplicar nueva clase
        if (contrast === 'high') {
            document.body.classList.add('high-contrast-mode');
        } else if (contrast === 'inverted') {
            document.body.classList.add('inverted-contrast-mode');
        }
    }

    updateActiveButtons() {
        const fontSize = localStorage.getItem('accessibility-fontSize') || 'normal';
        const contrast = localStorage.getItem('accessibility-contrast') || 'normal';

        // Actualizar botones de tamaño de fuente
        document.querySelectorAll('.font-size-btn').forEach(btn => {
            btn.classList.remove('active');
        });
        const fontActiveBtn = document.getElementById(`font-${fontSize}`);
        if (fontActiveBtn) {
            fontActiveBtn.classList.add('active');
            console.log('Active font button:', fontActiveBtn.id);
        }

        // Actualizar botones de contraste
        document.querySelectorAll('.contrast-btn').forEach(btn => {
            btn.classList.remove('active');
        });
        const contrastActiveBtn = document.getElementById(`contrast-${contrast}`);
        if (contrastActiveBtn) {
            contrastActiveBtn.classList.add('active');
            console.log('Active contrast button:', contrastActiveBtn.id);
        }
    }

    resetOptions() {
        console.log('Resetting accessibility options');
        localStorage.removeItem('accessibility-fontSize');
        localStorage.removeItem('accessibility-contrast');

        // Resetear a valores por defecto
        this.applyFontSize('normal');
        this.applyContrast('normal');
        this.updateActiveButtons();

        console.log('All options reset to default');
    }
}

// Inicialización cuando el DOM esté listo
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => {
        window.accessibility = new AccessibilityOptions();
        console.log('Accessibility system loaded');
    });
} else {
    window.accessibility = new AccessibilityOptions();
    console.log('Accessibility system loaded (DOM already ready)');
}