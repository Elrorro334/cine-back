# Cine Teatro Xilotzin - Backend (`cine-back`)

Backend desarrollado con **.NET 10.0 Web API**, **Entity Framework Core** y **SQL Server 2022**, siguiendo los principios de Clean Architecture. Provee servicios para taquilla, dulcería, cartelera, preórdenes y dashboard financiero.

---

## 📋 Requisitos Previos

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [SQL Server 2022](https://www.microsoft.com/sql-server/) (o LocalDB)
- [Java 17+](https://adoptium.net/) (requerido únicamente para análisis local con SonarScanner)
- Herramienta global de EF Core:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

---

## 🗄️ Guía de Entity Framework Core (Versionamiento de BD)

### 1. Instalación de Herramientas y Paquetes
Para habilitar el control de migraciones en el proyecto:
```bash
dotnet tool install --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design
```

### 2. Creación de Migraciones
Las migraciones se almacenan en el proyecto de infraestructura (`Cine.Infrastructure`):
```bash
# Migración 1: Creación inicial del esquema de base de datos
dotnet ef migrations add M1_CreacionInicial --project Cine.Infrastructure

# Migración 2: Control de cambios sobre cliente
dotnet ef migrations add M2_AgregarColumnaCliente --project Cine.Infrastructure

# Migración 3: Creación de índices de rendimiento y unicidad
dotnet ef migrations add M3_AgregarIndiceCorreo --project Cine.Infrastructure
```

### 3. Aplicar Migraciones a la Base de Datos
Para sincronizar y aplicar todas las migraciones pendientes hacia la base de datos SQL Server configurada en `appsettings.json`:
```bash
dotnet ef database update
```

Para verificar el historial de migraciones aplicadas en SQL Server:
```sql
SELECT * FROM __EFMigrationsHistory;
```

### 4. Rollback (Deshacer Migraciones) y Reaplicación del Cambio
Entity Framework Core permite regresar a cualquier migración previa especificando su nombre, y posteriormente reaplicar las migraciones pendientes:

```bash
# 1. Rollback: Revertir cambios hasta la migración M2 (deshace M3)
dotnet ef database update M2_AgregarColumnaCliente --project Cine.Infrastructure --startup-project .

# 2. Reaplicación del cambio: Detecta que M3 está pendiente en el historial de código y la vuelve a ejecutar
dotnet ef database update --project Cine.Infrastructure --startup-project .
```

### 5. Ejecución de Todas las Migraciones desde una Base de Datos Limpia (Desde Cero)
Cuando se despliega el proyecto en un entorno nuevo o se requiere reconstruir la base de datos `cineDataBase` completamente limpia desde cero aplicando secuencialmente `M1_CreacionInicial` $\rightarrow$ `M2_AgregarColumnaCliente` $\rightarrow$ `M3_AgregarIndiceCorreo`:

1. **(Opcional) Eliminar la base de datos existente para dejar el entorno limpio:**
   ```bash
   dotnet ef database drop --force --project Cine.Infrastructure --startup-project .
   ```
2. **Crear la base de datos desde cero y aplicar todas las migraciones en orden (`M1` -> `M2` -> `M3`):**
   ```bash
   dotnet ef database update --project Cine.Infrastructure --startup-project .
   ```
   > **¿Qué hace internamente EF Core en una base limpia?**
   > 1. Verifica que la base de datos `cineDataBase` no existe en la instancia de SQL Server y ejecuta `CREATE DATABASE [cineDataBase]`.
   > 2. Crea la tabla de auditoría `[__EFMigrationsHistory]`.
   > 3. Ejecuta en orden cronológico cada migración registrada en `Cine.Infrastructure/Migrations`:
   >    - `20261022172515_M1_CreacionInicial` (creación de tablas base, llaves primarias y foráneas).
   >    - `20261022172629_M2_AgregarColumnaCliente` (modificación estructural).
   >    - `20261022172730_M3_AgregarIndiceCorreo` (aplicación de índice y restricción).
   > 4. Registra los tres identificadores en `[__EFMigrationsHistory]` dejando el esquema en su versión más reciente.

3. **Verificar que las 3 migraciones quedaron aplicadas en SQL Server:**
   ```bash
   sqlcmd -S localhost -d cineDataBase -E -Q "SELECT MigrationId, ProductVersion FROM __EFMigrationsHistory;"
   ```

---

## 🔍 Guía de Análisis de Calidad con SonarCloud

El repositorio cuenta con integración continua configurada en `.github/workflows/sonarcloud.yml` para analizar automáticamente cada `push` y `pull request`.

### 1. Ejecución Local de SonarCloud
Para ejecutar el análisis estático de código y reporte de cobertura de forma local:

1. **Instalar la herramienta de SonarScanner:**
   ```bash
   dotnet tool install --global dotnet-sonarscanner
   ```

2. **Iniciar el escaneo (Begin):**
   ```bash
   dotnet-sonarscanner begin \
     /k:"elrorro334_cine-back" \
     /o:"elrorro334" \
     /d:sonar.token="TU_SONAR_TOKEN" \
     /d:sonar.host.url="https://sonarcloud.io" \
     /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml" \
     /d:sonar.coverage.exclusions="**/Program.cs,**/Controllers/TestController.cs,**/Migrations/**" \
     /d:sonar.cpd.exclusions="**/Migrations/**" \
     /d:sonar.exclusions="**/Migrations/**"
   ```

3. **Compilar la solución:**
   ```bash
   dotnet build --configuration Release
   ```

4. **Ejecutar pruebas unitarias recolectando cobertura OpenCover:**
   ```bash
   dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage;Format=opencover"
   ```

5. **Finalizar y enviar el análisis a SonarCloud (End):**
   ```bash
   dotnet-sonarscanner end /d:sonar.token="TU_SONAR_TOKEN"
   ```

### 2. Ejecución Automática (CI/CD)
En GitHub Actions el análisis se dispara automáticamente con los secretos `SONAR_TOKEN` y `GITHUB_TOKEN`. Para aprobar el Quality Gate se requiere:
- 0 Bugs
- 0 Vulnerabilidades
- Cobertura de pruebas unitarias superior al **80%** en código nuevo
- Deuda técnica bajo control (Code Smells)

---

## 🚀 Ejecución de la API en Desarrollo

1. Configurar la cadena de conexión en `appsettings.Development.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=cineDataBase;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```
2. Ejecutar la aplicación:
   ```bash
   dotnet run
   ```
3. Acceder a Swagger en el navegador:
   `https://localhost:7088/swagger`
