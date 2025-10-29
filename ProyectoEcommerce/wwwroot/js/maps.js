// Variables globales
let map;
let markers = [];
let infoWindow;
let userMarker;
let branches = [];

// Inicializar el mapa
function initMap() {
    try {
        console.log('Inicializando mapa...');

        // Coordenadas centrales (Universidad Nacional)
        const center = { lat: 10.134533, lng: -85.446590 };

        // Crear el mapa
        map = new google.maps.Map(document.getElementById('map'), {
            zoom: 13,
            center: center,
            mapTypeControl: true,
            streetViewControl: true,
            fullscreenControl: true,
            mapTypeControlOptions: {
                style: google.maps.MapTypeControlStyle.HORIZONTAL_BAR,
                position: google.maps.ControlPosition.TOP_RIGHT
            },
            zoomControl: true,
            zoomControlOptions: {
                position: google.maps.ControlPosition.RIGHT_CENTER
            }
        });

        // Ocultar mensaje de carga
        const loadingElement = document.querySelector('.map-loading');
        if (loadingElement) {
            loadingElement.style.display = 'none';
        }

        // Crear ventana de información
        infoWindow = new google.maps.InfoWindow();

        // Obtener todas las sucursales
        const branchCards = document.querySelectorAll('.branch-card');
        branches = Array.from(branchCards).map(card => {
            return {
                id: card.dataset.branch,
                name: card.querySelector('.branch-name').textContent,
                lat: parseFloat(card.dataset.lat),
                lng: parseFloat(card.dataset.lng),
                address: card.querySelector('.branch-address').textContent,
                hours: card.querySelector('.branch-hours').textContent,
                phone: card.querySelector('.branch-phone').textContent,
                payment: card.querySelector('.branch-payment').textContent,
                element: card
            };
        });

        // Añadir marcadores para cada sucursal
        branches.forEach(branch => {
            addMarker(branch);
        });

        // Configurar eventos para las tarjetas de sucursal
        setupBranchInteractions();

        // Configurar botones del mapa
        const locateButton = document.getElementById('locate-me');
        if (locateButton) {
            locateButton.addEventListener('click', locateUser);
        }

        // Configurar barra de búsqueda
        setupSearch();

        console.log('Mapa inicializado correctamente');

    } catch (error) {
        console.error('Error al inicializar el mapa:', error);
        showMapError('Error: ' + error.message);
    }
}

// Mostrar error del mapa
function showMapError(message) {
    console.error(message);
    // Puedes mostrar un mensaje de error en la página si quieres
}

// Añadir marcador al mapa
function addMarker(branch) {
    const marker = new google.maps.Marker({
        position: { lat: branch.lat, lng: branch.lng },
        map: map,
        title: branch.name,
        animation: google.maps.Animation.DROP
    });

    // Crear contenido para la ventana de información
    const content = `
        <div class="map-info-window">
            <h3>${branch.name}</h3>
            <p>${branch.address}</p>
            <p>${branch.hours}</p>
            <p>${branch.phone}</p>
            <p>${branch.payment}</p>
        </div>
    `;

    // Añadir evento de clic al marcador
    marker.addListener('click', () => {
        infoWindow.setContent(content);
        infoWindow.open(map, marker);

        // Resaltar la tarjeta correspondiente
        highlightBranchCard(branch.id);
    });

    markers.push(marker);
}

// Configurar interacciones con las tarjetas de sucursal
function setupBranchInteractions() {
    branches.forEach(branch => {
        branch.element.addEventListener('click', () => {
            // Centrar el mapa en la sucursal
            map.setCenter({ lat: branch.lat, lng: branch.lng });
            map.setZoom(15);

            // Abrir la ventana de información del marcador
            const marker = markers.find(m => m.title === branch.name);
            if (marker) {
                const content = `
                    <div class="map-info-window">
                        <h3>${branch.name}</h3>
                        <p>${branch.address}</p>
                        <p>${branch.hours}</p>
                        <p>${branch.phone}</p>
                        <p>${branch.payment}</p>
                    </div>
                `;
                infoWindow.setContent(content);
                infoWindow.open(map, marker);
            }

            // Resaltar la tarjeta
            highlightBranchCard(branch.id);
        });
    });
}

// Resaltar la tarjeta de sucursal
function highlightBranchCard(branchId) {
    // Quitar la clase active de todas las tarjetas
    branches.forEach(branch => {
        branch.element.classList.remove('active');
    });

    // Añadir la clase active a la tarjeta seleccionada
    const selectedBranch = branches.find(branch => branch.id === branchId);
    if (selectedBranch) {
        selectedBranch.element.classList.add('active');
    }
}

// Ubicar al usuario
function locateUser() {
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(
            (position) => {
                const userLocation = {
                    lat: position.coords.latitude,
                    lng: position.coords.longitude
                };

                // Centrar el mapa en la ubicación del usuario
                map.setCenter(userLocation);
                map.setZoom(14);

                // Crear o mover el marcador del usuario
                if (userMarker) {
                    userMarker.setPosition(userLocation);
                } else {
                    userMarker = new google.maps.Marker({
                        position: userLocation,
                        map: map,
                        title: 'Tu ubicación',
                        icon: {
                            path: google.maps.SymbolPath.CIRCLE,
                            scale: 10,
                            fillColor: '#4285F4',
                            fillOpacity: 1,
                            strokeColor: '#FFFFFF',
                            strokeWeight: 2
                        }
                    });
                }

                // Calcular distancias a las sucursales
                calculateDistances(userLocation);
            },
            (error) => {
                alert('No se pudo obtener tu ubicación: ' + error.message);
            }
        );
    } else {
        alert('La geolocalización no es compatible con este navegador.');
    }
}

// Calcular distancias a las sucursales
function calculateDistances(userLocation) {
    const service = new google.maps.DistanceMatrixService();

    const origins = [userLocation];
    const destinations = branches.map(branch => ({ lat: branch.lat, lng: branch.lng }));

    service.getDistanceMatrix({
        origins: origins,
        destinations: destinations,
        travelMode: google.maps.TravelMode.DRIVING,
        unitSystem: google.maps.UnitSystem.METRIC
    }, (response, status) => {
        if (status === 'OK') {
            const results = response.rows[0].elements;

            results.forEach((result, index) => {
                if (result.status === 'OK') {
                    const distance = result.distance.text;
                    const branch = branches[index];

                    // Actualizar la distancia en la tarjeta
                    const distanceElement = branch.element.querySelector('.branch-distance');
                    if (distanceElement) {
                        distanceElement.textContent = `A ${distance}`;
                    }
                }
            });
        }
    });
}

// Configurar barra de búsqueda
function setupSearch() {
    const searchInput = document.getElementById('branch-search');

    if (searchInput) {
        searchInput.addEventListener('input', () => {
            const searchTerm = searchInput.value.toLowerCase();

            branches.forEach(branch => {
                const branchText = `${branch.name} ${branch.address} ${branch.hours}`.toLowerCase();
                const isVisible = branchText.includes(searchTerm);

                branch.element.style.display = isVisible ? 'block' : 'none';
            });
        });
    }
}

// Manejar errores de la API de Google Maps
window.gm_authFailure = function () {
    alert('Error al cargar Google Maps. Por favor, verifica tu API key.');
};