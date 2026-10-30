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

`WebhooksController` verifica la firma de Stripe antes de procesar `checkout.session.completed`. La actualización de estado e inventario requiere coherencia transaccional e idempotencia frente a reintentos y concurrencia. Antes de habilitar pagos reales, valida el stock disponible, evita descuentos duplicados y prueba fallos intermedios durante la confirmación. Este flujo requiere validación de integración.

Los logs generados y los resultados locales de compilación no forman parte del código fuente ni deben incluir información de pedidos o usuarios.
