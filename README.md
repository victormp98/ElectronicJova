# ElectronicJova

Tienda web con ASP.NET Core 8 MVC, Identity, Entity Framework Core, MySQL, Stripe Checkout y SignalR.

## Funcionalidad

- Catálogo, categorías y variantes de productos.
- Carrito de compra con actualización de cantidades y precios.
- Roles administrativos y de cliente mediante ASP.NET Core Identity.
- Creación y seguimiento de pedidos.
- Sesiones Stripe Checkout y recepción de eventos de pago.
- Notificaciones de cambio de estado mediante SignalR.

## Componentes

- `ElectronicJova/Areas/Admin/`: catálogo, administración y operaciones de pedidos.
- `ElectronicJova/Areas/Customer/`: tienda y carrito.
- `ElectronicJova/Areas/Identity/`: registro e inicio de sesión.
- `ElectronicJova/Controllers/WebhooksController.cs`: eventos de Stripe.
- `ElectronicJova/Data/`: contexto EF Core, repositorios y migraciones.
- `ElectronicJova.UnitTests/`: pruebas de precios, carrito y repositorios.

## Dependencias

- .NET SDK 8
- MySQL compatible con el proveedor Pomelo de Entity Framework Core
- Cuenta de Stripe con claves de prueba para ejecutar el checkout
- Configuración de correo transaccional para confirmaciones

## Configuración local

`ElectronicJova/ElectronicJova.csproj` define `UserSecretsId` para configuración privada durante el desarrollo. Establece estos valores sin incluir secretos en archivos versionados:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<CONEXION_MYSQL_LOCAL>" --project ElectronicJova/ElectronicJova.csproj
dotnet user-secrets set "StripeSettings:SecretKey" "<CLAVE_STRIPE_TEST>" --project ElectronicJova/ElectronicJova.csproj
dotnet user-secrets set "StripeSettings:WebhookSecret" "<SECRETO_WEBHOOK_TEST>" --project ElectronicJova/ElectronicJova.csproj
dotnet user-secrets set "ResendSettings:ApiKey" "<TOKEN_CORREO_TEST>" --project ElectronicJova/ElectronicJova.csproj
```

Los valores entre `<...>` son marcadores de posición. Nunca utilices claves reales en un commit.

## Compilación y pruebas

```powershell
dotnet build ElectronicJova.slnx
dotnet test ElectronicJova.slnx
dotnet run --project ElectronicJova/ElectronicJova.csproj
```

## Procesamiento de pagos

`WebhooksController` valida la firma del evento `checkout.session.completed` mediante la biblioteca de Stripe y procesa únicamente sesiones con estado `paid`.

El servicio inicia una transacción de base de datos con aislamiento `Serializable`, obtiene el pedido y sus partidas, agrupa cantidades por producto y verifica existencias antes de modificar el inventario. La actualización de estado, los descuentos y el registro de transición comparten un mismo `SaveChangesAsync` y `CommitAsync`.

Un pedido con estado `PaymentStatusApproved` responde sin repetir el descuento. Cuando faltan existencias o no existe el pedido, el webhook devuelve `503`, registra la incidencia y permite el reintento de Stripe. La intervención operativa puede ser necesaria para resolver inconsistencias de stock.

SignalR y el correo se ejecutan después de confirmar la transacción. Los errores de notificación no revierten el estado del pedido.

### Pruebas de integración recomendadas

1. Webhook válido con stock suficiente: estado aprobado y descuento único.
2. Evento repetido: inventario sin cambios adicionales.
3. Dos webhooks concurrentes: un único descuento efectivo.
4. Varias líneas del mismo producto: validar cantidad total agrupada.
5. Stock insuficiente, pedido inexistente y fallo de persistencia: transacción revertida.
6. Firma de Stripe inválida: HTTP 400 sin modificar pedidos.
7. Error de SignalR o SMTP después del commit: conservar el pedido aprobado.

Para validar concurrencia y transacciones utiliza el mismo proveedor MySQL configurado en el despliegue, no únicamente pruebas con dobles en memoria.
