# 📖 Instructivo del Proyecto: ElectronicJova

¡Bienvenido al manual de ElectronicJova! Este documento explica cómo funciona el sistema, desde la gestión administrativa hasta el flujo de compra del cliente.

## 🚀 Inicio Rápido

El proyecto está construido con **ASP.NET Core 8**, utilizando una arquitectura de repositorios y una base de datos **MySQL**.

### Áreas del Sistema
1. **Área de Cliente (Customer)**: Donde los usuarios ven productos, los añaden al carrito y realizan compras.
2. **Área de Administrador (Admin)**: Panel de control para gestionar el catálogo, categorías y pedidos.
3. **Área de Identidad (Identity)**: Manejo de login, registro y perfiles de usuario.

---

## 🛒 Flujo de Compra

1. **Catálogo**: El usuario explora productos en la página principal o mediante la **Búsqueda Avanzada**.
2. **Detalles**: Al seleccionar un producto, se pueden elegir opciones (ej. color, capacidad) y añadir notas especiales.
3. **Carrito**: El sistema utiliza **AJAX** para sumar/restar cantidades sin recargar la página, manteniendo una navegación fluida.
4. **Pago (Stripe)**: Al proceder al pago, se crea una sesión segura en Stripe. El sistema está configurado para manejar transacciones en **Pesos Mexicanos (MXN)**.
5. **Confirmación**: Una vez completado el pago, el sistema recibe un Webhook de Stripe que confirma la orden y descuenta el stock automáticamente.

---

## 🛠️ Gestión Administrativa

Para acceder como administrador, debes iniciar sesión con una cuenta que tenga el rol de **Admin**.

- **Categorías**: Permite agrupar productos. Cada categoría puede tener un nombre y un orden de visualización.
- **Productos**: Gestión total del catálogo.
    - **Imágenes**: Se almacenan en `wwwroot/images/products`.
    - **Stock**: El sistema bloquea compras si no hay existencias.
    - **Opciones**: Se pueden añadir variaciones con precios adicionales.
- **Pedidos**: El administrador puede rastrear el estado de cada orden (Pendiente, Procesando, Enviado, Cancelado).

---

## ⚙️ Configuración Técnica

### Base de Datos
La conexión se configura en `appsettings.json`. El sistema incluye un **DbInitializer** que crea los roles básicos (Admin, Customer) y datos de prueba si la base de datos está vacía.

### Pagos (Stripe)
Requiere las llaves `PublishableKey` y `SecretKey` en la sección `StripeSettings`. Es vital configurar el **Webhook Secret** para que el sistema sepa cuándo un pago fue exitoso fuera del sitio.

### Localización
El sistema está forzado a la cultura `es-MX`. Esto asegura que las fechas, números y símbolos de moneda ($) sigan el estándar mexicano.

---

## 🧪 Pruebas de Compra

Para realizar pruebas sin usar dinero real, el sistema debe estar configurado con las **API Keys de prueba** de Stripe (`pk_test_...` y `sk_test_...`).

### Datos de Tarjeta de Prueba (Stripe)
Puedes usar la siguiente tarjeta universal para simular una compra exitosa:

- **Número**: `4242 4242 4242 4242`
- **Fecha**: Cualquier fecha futura (ej. `12/30`)
- **CVC**: `123`
- **CP**: Cualquier código postal (ej. `06000`)

### Pasos para Probar:
1. Añade productos al carrito.
2. Haz clic en **"Proceder al Pago"**.
3. En la pantalla de Stripe, ingresa los datos de la tarjeta mencionados arriba.
4. Tras pagar, serás redirigido a la página de **Confirmación de Orden**.

---
*Documento generado para la presentación del proyecto ElectronicJova.*
