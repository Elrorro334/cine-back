# AGENTS.md — Backend Cine Teatro Xilotzin (`cine-back`)

**Arquitectura:** Clean Architecture en .NET 10.0 Web API + Entity Framework Core + SQL Server 2022.

---

## 1. Principio Fundamental de Autoridad (Cero Lógica en Next.js)

1. **El Backend .NET Core es la única autoridad de negocio:**
   - Todo cálculo contable, validación de stock, generación de folios (`VT-XXXXX`), asignación de butacas por función y validación de vigencia de pre-órdenes reside **exclusivamente** en este backend.
   - El frontend Next.js es una capa de presentación pura (*Dumb UI / Presentation View*). Nunca delegar al frontend cálculos de totales, estados de asientos o generación de números de ticket.
2. **Persistencia Real en SQL Server (`cineDataBase`):**
   - Toda venta confirmada se persiste en `venta` y `detalle_venta`.
   - La ocupación de butacas se persiste en `estado_butaca_funcion` con `estato = true`.
   - Las existencias de dulcería se descuentan en `producto.stock_actual`.
   - Las promociones y cartelera se sirven dinámicamente desde los controladores REST.
3. **Restricción de Emojis:**
   - Los DTOs de salida no deben emitir emojis Unicode como íconos; deben retornar clases de íconos vectoriales como `fa-ticket`, `fa-clapperboard`, `fa-qrcode`.
