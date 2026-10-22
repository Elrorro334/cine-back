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

### 4. Rollback (Deshacer Migraciones)
Entity Framework permite regresar a cualquier migración previa especificando su nombre:
```bash
# Revertir cambios hasta la migración M2 (deshace M3)
dotnet ef database update M2_AgregarColumnaCliente

# Regresar la base de datos al estado completamente actualizado
dotnet ef database update
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
     /d:sonar.coverage.exclusions="**/Program.cs,**/Controllers/TestController.cs,**/Migrations/**"
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
