# ElectronicJova

Proyecto de comercio electrónico con ASP.NET Core, Identity, MySQL, integración con Stripe y notificaciones SignalR. Se trata de un proyecto de portafolio; **no se declara listo para operación comercial en producción**.

## Estado de revisión

- El repositorio contiene pruebas unitarias en `ElectronicJova.UnitTests`.
- Para ejecutar el proyecto, configura de forma privada la conexión a base de datos, el correo transaccional y las credenciales de Stripe; no publiques secretos en archivos rastreados.
- **Pendiente crítico:** el procesamiento del webhook en `ElectronicJova/Controllers/WebhooksController.cs` confirma el pago y descuenta inventario mediante guardados separados. La implementación actual registra stock insuficiente y continúa el descuento. Antes de usar pagos reales, implementar operaciones transaccionales, manejo de concurrencia e idempotencia, y probar reintentos del webhook.
- Los registros y resultados de compilación generados se han excluido de esta rama de limpieza. Eliminarlos del árbol actual no los elimina del historial de Git.

## Revisión manual antes de publicar

1. Configurar credenciales locales fuera del repositorio.
2. Ejecutar compilación y pruebas unitarias.
3. Revisar migraciones, métodos administrativos y flujos de pago.
4. No habilitar pagos reales hasta resolver el problema de inventario.
