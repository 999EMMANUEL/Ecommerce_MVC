# Configuración de Envío de Facturas por Email

## Problemas Corregidos

### Última actualización: 2025-11-15

### 1. ✅ Validación Mejorada de Datos Antes del Envío
**Problema:** El método `SendInvoiceEmailAsync` podía recibir objetos `Buy` sin las relaciones necesarias (`Items`, `Customer`) cargadas, causando errores al renderizar la vista.

**Solución:** Agregadas validaciones exhaustivas que verifican:
- El objeto `Buy` no sea null
- El email del destinatario sea válido
- Los `Items` de la compra estén cargados con `.Include()`
- El `Customer` esté cargado con `.Include()`
- La configuración de email esté completa

### 2. ✅ Logging Extensivo para Debugging
**Problema:** Era difícil diagnosticar por qué los emails no se enviaban.

**Solución:** Agregado logging detallado en múltiples puntos:
- Validación de parámetros de entrada
- Verificación de datos cargados en `Buy`
- Configuración de SMTP (host, puerto, usuario)
- Estado de generación de HTML y PDF
- Proceso de envío SMTP
- Errores específicos con detalles completos

### 3. ✅ Mensajes de Error Visibles al Usuario
**Problema:** Los errores del email se capturaban pero el usuario no veía los detalles.

**Solución:**
- Los errores ahora se muestran en `TempData["Warning"]` con el detalle completo
- Incluye tanto el error principal como `InnerException` si existe
- El usuario puede ver exactamente qué salió mal

### 4. ✅ Error Crítico en EmailService.cs (Corregido Previamente)
**Problema:** El `MemoryStream` del PDF se estaba disponiendo antes de que el email se enviara, causando que el attachment no pudiera leer los datos.

**Solución:** Reestructurado el código para mantener el stream vivo hasta después del envío:
```csharp
// Antes (INCORRECTO):
using (var pdfStream = new MemoryStream(pdfBytes))
{
    var attachment = new Attachment(pdfStream, ...);
    message.Attachments.Add(attachment);

    using (var smtpClient = ...)
    {
        await smtpClient.SendMailAsync(message);
    }
} // El stream se cierra aquí, ANTES de que el attachment se use

// Ahora (CORRECTO):
var pdfStream = new MemoryStream(pdfBytes);
try
{
    var attachment = new Attachment(pdfStream, ...);
    message.Attachments.Add(attachment);

    using (var smtpClient = ...)
    {
        await smtpClient.SendMailAsync(message);
    }
}
finally
{
    pdfStream?.Dispose(); // Se cierra DESPUÉS del envío
}
```

### 5. ✅ Logging Mejorado (Actualizado Hoy)
Ahora el servicio registra información detallada sobre:
- Validación de configuración
- Validación de datos de entrada (Buy, Items, Customer)
- Generación de HTML y PDF
- Configuración del mensaje
- Proceso de envío SMTP
- Errores específicos (SMTP vs. generales)
- **NUEVO:** Configuración de SMTP (host, port, username, email)
- **NUEVO:** Validaciones previas antes de llamar EmailService

### 6. ✅ Timeout SMTP (Corregido Previamente)
Agregado timeout de 30 segundos para evitar bloqueos indefinidos.

---

## Configuración de Gmail (IMPORTANTE)

### Paso 1: Verificar Contraseña de Aplicación

La configuración actual usa:
```json
"Username": "ejj56810@gmail.com",
"Password": "lnlf ywrd zsqh mkgv"
```

**Esta NO es tu contraseña normal de Gmail.** Es una "Contraseña de aplicación" que debes generar.

### Paso 2: Generar Nueva Contraseña de Aplicación de Gmail

1. **Ir a tu cuenta de Google:**
   - Visita: https://myaccount.google.com/

2. **Habilitar verificación en 2 pasos (si no está habilitada):**
   - Ve a "Seguridad" → "Verificación en 2 pasos"
   - Sigue las instrucciones para habilitarla
   - ⚠️ **REQUERIDO:** No puedes crear contraseñas de aplicación sin esto

3. **Crear contraseña de aplicación:**
   - Ve a: https://myaccount.google.com/apppasswords
   - O navega: "Seguridad" → "Contraseñas de aplicaciones"
   - Selecciona "Correo" y "Otro (nombre personalizado)"
   - Escribe: "InnovaTech Ecommerce"
   - Haz clic en "Generar"
   - **Copia la contraseña de 16 caracteres** (con espacios: "xxxx xxxx xxxx xxxx")

4. **Actualizar appsettings.json:**
   ```json
   "MailSettings": {
     "SmtpHost": "smtp.gmail.com",
     "SmtpPort": 587,
     "SenderName": "InnovaTech",
     "SenderEmail": "ejj56810@gmail.com",
     "Username": "ejj56810@gmail.com",
     "Password": "TU_NUEVA_CONTRASEÑA_AQUI"
   }
   ```

### Paso 3: Verificar Configuración de Seguridad de Gmail

1. **Acceso de aplicaciones menos seguras:**
   - Gmail ya NO usa esta opción (obsoleto)
   - Ahora SOLO funcionan las contraseñas de aplicación

2. **Verificar que no haya bloqueos:**
   - Ve a: https://myaccount.google.com/notifications
   - Verifica si hay alertas de seguridad sobre intentos de inicio de sesión bloqueados
   - Si hay bloqueos, autoriza el acceso

---

## Verificar los Logs

Con los cambios implementados, ahora puedes ver logs detallados en la consola de tu aplicación:

### Logs exitosos:
```
info: ProyectoEcommerce.Services.EmailService[0]
      Iniciando envío de factura por email para orden 123 a cliente@ejemplo.com
info: ProyectoEcommerce.Services.EmailService[0]
      Enviando email a través de SMTP smtp.gmail.com:587...
info: ProyectoEcommerce.Services.EmailService[0]
      Email enviado exitosamente a cliente@ejemplo.com para orden 123
```

### Logs de error comunes:

**Error de autenticación:**
```
fail: ProyectoEcommerce.Services.EmailService[0]
      Error SMTP al enviar email. StatusCode: GeneralFailure
      System.Net.Mail.SmtpException: The SMTP server requires a secure connection or the client was not authenticated.
```
**Solución:** Verifica tu contraseña de aplicación

**Error de conexión:**
```
fail: ProyectoEcommerce.Services.EmailService[0]
      Error SMTP al enviar email. StatusCode: ServiceNotAvailable
```
**Solución:** Verifica tu conexión a internet o firewall

---

## Probar el Envío de Emails

### Método 1: Realizar una compra de prueba
1. Ejecuta la aplicación: `dotnet run`
2. Navega al sitio y agrega productos al carrito
3. Completa el proceso de pago
4. Verifica en la consola los logs del envío de email
5. Revisa tu bandeja de entrada

### Método 2: Ver logs en tiempo real
```bash
dotnet run --environment Development
```

La aplicación mostrará logs detallados en la consola cuando se intente enviar un email.

---

## Solución de Problemas Comunes

### Problema: No llegan emails

**Posibles causas:**

1. **Contraseña de aplicación incorrecta o expirada**
   - Genera una nueva contraseña de aplicación
   - Actualiza `appsettings.json`

2. **Verificación en 2 pasos no habilitada**
   - Habilita la verificación en 2 pasos en tu cuenta de Google
   - Luego crea la contraseña de aplicación

3. **Email en carpeta de spam**
   - Revisa la carpeta de spam/correo no deseado del destinatario
   - Agrega ejj56810@gmail.com a contactos

4. **Firewall bloqueando puerto 587**
   - Verifica que tu servidor permita conexiones salientes al puerto 587
   - Prueba cambiar a puerto 465 con SSL directo (requiere cambios en código)

5. **Límites de Gmail**
   - Gmail tiene límite de ~500 emails/día para cuentas gratuitas
   - Verifica: https://myaccount.google.com/activity

### Problema: Error "The SMTP server requires a secure connection"

**Solución:** Asegúrate de que:
- `SmtpPort` es 587 (STARTTLS)
- `EnableSsl` está en `true`
- Tu contraseña de aplicación es correcta

### Problema: Timeout al enviar

**Posibles causas:**
- Conexión a internet lenta
- Firewall bloqueando Gmail
- Proxy corporativo interfiriendo

**Solución:**
- Aumenta el timeout en `EmailService.cs` línea 96:
  ```csharp
  smtpClient.Timeout = 60000; // 60 segundos
  ```

---

## Configuración Alternativa (Gmail Workspace)

Si usas Gmail Workspace (G Suite), puedes necesitar configuración adicional:

1. **Habilitar SMTP en Admin Console:**
   - Ve a: admin.google.com
   - Apps → Google Workspace → Gmail → Routing
   - Habilita "SMTP relay"

2. **Usar autenticación OAuth2 (recomendado para producción):**
   - Requiere implementar flujo OAuth2
   - Más seguro que contraseñas de aplicación
   - Consulta: https://developers.google.com/gmail/api/guides/sending

---

## Configuración para Producción

### Recomendaciones:

1. **NO subas contraseñas a Git:**
   - Usa variables de entorno
   - Usa Azure Key Vault o AWS Secrets Manager

   ```csharp
   // En Program.cs:
   builder.Configuration.AddEnvironmentVariables();

   // Luego en tu servidor:
   export MailSettings__Password="tu_contraseña_aqui"
   ```

2. **Usa un servicio de email profesional:**
   - SendGrid
   - Amazon SES
   - Mailgun
   - Postmark

   Estos servicios ofrecen:
   - Mayor límite de envíos
   - Mejor entregabilidad
   - Analytics y reportes
   - Menos probabilidad de acabar en spam

3. **Implementa reintentos:**
   ```csharp
   // Ejemplo con Polly (paquete NuGet)
   var retryPolicy = Policy
       .Handle<SmtpException>()
       .WaitAndRetryAsync(3, retryAttempt =>
           TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

   await retryPolicy.ExecuteAsync(async () =>
   {
       await smtpClient.SendMailAsync(message);
   });
   ```

---

## Checklist de Verificación

- [ ] Verificación en 2 pasos habilitada en Gmail
- [ ] Contraseña de aplicación generada
- [ ] appsettings.json actualizado con nueva contraseña
- [ ] Aplicación reiniciada después de cambiar configuración
- [ ] Logs revisados para ver mensajes de error específicos
- [ ] Bandeja de spam revisada
- [ ] Puerto 587 no bloqueado por firewall
- [ ] Email de prueba enviado y recibido

---

## Archivos Modificados

### Actualización 2025-11-15:

1. **EmailService.cs** (ProyectoEcommerce/Services/)
   - ✅ **NUEVO:** Validación exhaustiva de parámetros de entrada (`buy`, `recipientEmail`)
   - ✅ **NUEVO:** Validación de relaciones cargadas (`Items`, `Customer`)
   - ✅ **NUEVO:** Logging de configuración SMTP al iniciar envío
   - ✅ Corregido manejo de MemoryStream para attachments (previo)
   - ✅ Agregado logging detallado en múltiples puntos
   - ✅ Agregado timeout SMTP de 30 segundos
   - ✅ Validación de configuración mejorada
   - ✅ Mejor manejo de excepciones con mensajes descriptivos

2. **ShoppingCartsController.cs** (ProyectoEcommerce/Controllers/)
   - ✅ **NUEVO:** Validaciones previas antes de llamar `SendInvoiceEmailAsync`
   - ✅ **NUEVO:** Verificación de que `Items` y `Customer` estén cargados
   - ✅ **NUEVO:** Logging adicional antes y después del envío de email
   - ✅ **NUEVO:** Manejo de errores mejorado con `TempData["Warning"]` que muestra el error completo
   - ✅ **NUEVO:** Inclusión de `InnerException` en mensajes de error para mejor debugging

3. **CONFIGURACION_EMAIL.md** (Raíz del proyecto)
   - ✅ **NUEVO:** Documentación actualizada con los nuevos cambios
   - ✅ Instrucciones detalladas para configuración de Gmail
   - ✅ Checklist de verificación completo
   - ✅ Solución de problemas comunes

4. **appsettings.json**
   - ⚠️ **ACCIÓN REQUERIDA:** Actualiza la contraseña de aplicación si no funciona

---

## Soporte

Si después de seguir todos estos pasos aún tienes problemas:

1. **Revisa los logs** en la consola cuando hagas una compra
2. **Copia el mensaje de error completo**
3. **Verifica el StatusCode del error SMTP**
4. **Intenta con un email de prueba diferente** (no Gmail)

Errores comunes y sus códigos:
- `GeneralFailure`: Autenticación fallida
- `ServiceNotAvailable`: No se puede conectar a Gmail
- `MailboxUnavailable`: Email destinatario no existe
- `ExceededStorageAllocation`: Bandeja del destinatario llena
