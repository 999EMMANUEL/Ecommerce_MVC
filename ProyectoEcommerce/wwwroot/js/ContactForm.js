class InnovaTechFormValidator {
    constructor(formId) {
        this.form = document.getElementById(formId);
        this.initializeValidation();
    }

    initializeValidation() {
        // Validación en tiempo real
        this.form.addEventListener('input', (e) => {
            this.validateField(e.target);
        });

        // Validación al enviar
        this.form.addEventListener('submit', (e) => {
            e.preventDefault();
            this.handleSubmit();
        });

        // Formatear teléfono mientras se escribe
        document.getElementById('telefono').addEventListener('input', this.formatPhone);
    }

    validateField(field) {
        const value = field.value.trim();
        const fieldName = field.name;
        let isValid = true;
        let errorMessage = '';

        switch (fieldName) {
            case 'nombre':
                isValid = this.validateNombre(value);
                errorMessage = isValid ? '' : 'El nombre debe tener al menos 2 caracteres y solo contener letras y espacios';
                break;

            case 'email':
                isValid = this.validateEmail(value);
                errorMessage = isValid ? '' : 'Por favor ingresa un correo electrónico válido';
                break;

            case 'telefono':
                isValid = this.validateTelefono(value);
                errorMessage = isValid ? '' : 'Ingresa un número de teléfono válido (8-15 dígitos)';
                break;

            case 'mensaje':
                isValid = this.validateMensaje(value);
                errorMessage = isValid ? '' : 'El mensaje debe tener al menos 10 caracteres';
                break;
        }

        this.showFieldValidation(field, isValid, errorMessage);
        return isValid;
    }

    validateNombre(nombre) {
        const nombreRegex = /^[a-zA-ZÀ-ÿ\u00f1\u00d1\s]{2,50}$/;
        return nombre.length >= 2 && nombreRegex.test(nombre);
    }

    validateEmail(email) {
        const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        return emailRegex.test(email) && email.length <= 100;
    }

    validateTelefono(telefono) {
        const telefonoLimpio = telefono.replace(/\D/g, '');
        return telefonoLimpio.length >= 8 && telefonoLimpio.length <= 15;
    }

    validateMensaje(mensaje) {
        return mensaje.length >= 10 && mensaje.length <= 500;
    }

    showFieldValidation(field, isValid, errorMessage) {
        const errorElement = document.getElementById(field.name + 'Error');

        if (isValid) {
            field.classList.remove('error');
            field.classList.add('success');
            errorElement.style.display = 'none';
        } else {
            field.classList.remove('success');
            field.classList.add('error');
            errorElement.textContent = errorMessage;
            errorElement.style.display = 'block';
        }
    }

    validateAllFields() {
        const fields = this.form.querySelectorAll('input, textarea');
        let allValid = true;

        fields.forEach(field => {
            const isFieldValid = this.validateField(field);
            if (!isFieldValid) {
                allValid = false;
            }
        });

        return allValid;
    }

    async handleSubmit() {
        const submitBtn = document.getElementById('submitBtn');
        const successMessage = document.getElementById('successMessage');

        if (!this.validateAllFields()) {
            return;
        }

        // Cambiar botón a estado de carga
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<div class="contact-form-loading"></div>Enviando...';

        try {
            // Obtener token antiforgery
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            if (!token) {
                throw new Error('Token de seguridad no encontrado');
            }

            // Preparar datos del formulario
            const formData = {
                name: document.getElementById('nombre').value.trim(),
                email: document.getElementById('email').value.trim(),
                phone: document.getElementById('telefono').value.trim(),
                message: document.getElementById('mensaje').value.trim()
            };

            // Enviar datos al servidor
            const response = await fetch('/Contact/SendMessage', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify(formData)
            });

            const result = await response.json();

            if (response.ok && result.success) {
                // Mostrar mensaje de éxito
                successMessage.textContent = result.message || '¡Mensaje enviado correctamente! Te contactaremos pronto.';
                successMessage.style.display = 'block';
                this.form.reset();

                // Limpiar clases de validación
                const fields = this.form.querySelectorAll('input, textarea');
                fields.forEach(field => {
                    field.classList.remove('success', 'error');
                });

                // Scroll suave hacia el mensaje de éxito
                successMessage.scrollIntoView({ behavior: 'smooth', block: 'center' });

                setTimeout(() => {
                    successMessage.style.display = 'none';
                }, 5000);
            } else {
                throw new Error(result.message || 'Error al enviar el mensaje');
            }

        } catch (error) {
            console.error('Error:', error);
            alert(error.message || 'Error al enviar el formulario. Por favor intenta nuevamente.');
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerHTML = 'Enviar consulta';
        }
    }

    formatPhone(e) {
        let value = e.target.value.replace(/\D/g, '');

        // Formato para Costa Rica: +506 8888-8888
        if (value.length > 0) {
            if (value.length <= 8) {
                value = value.replace(/(\d{4})(\d{0,4})/, '$1-$2');
            }
            if (value.startsWith('506')) {
                value = '+' + value.replace(/(\d{3})(\d{4})(\d{0,4})/, '$1 $2-$3');
            } else if (value.length === 8) {
                value = '+506 ' + value.replace(/(\d{4})(\d{4})/, '$1-$2');
            }
        }

        e.target.value = value;
    }
}

// Inicializar validador cuando el DOM esté listo
document.addEventListener('DOMContentLoaded', () => {
    new InnovaTechFormValidator('contactForm');
});