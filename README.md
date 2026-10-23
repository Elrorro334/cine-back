# Cine Teatro Xilotzin — Backend (`cine-back`)

Backend oficial y autoridad central de negocio para el sistema de taquilla, dulcería, cartelera, preórdenes y dashboard financiero del **Cine Teatro Xilotzin**.

Desarrollado con **.NET 10.0 Web API**, **Entity Framework Core 10** y **SQL Server 2022**, siguiendo los principios de **Clean Architecture (Arquitectura Limpia)**.

[![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Entity Framework Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4?logo=dotnet&logoColor=white)](https://learn.microsoft.com/ef/core/)
[![SQL Server 2022](https://img.shields.io/badge/SQL%20Server-2022-CC292B?logo=microsoftsqlserver&logoColor=white)](https://www.microsoft.com/sql-server/)
[![SonarQube Cloud](https://sonarcloud.io/api/project_badges/measure?project=elrorro334_cine-back&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=elrorro334_cine-back)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=elrorro334_cine-back&metric=coverage)](https://sonarcloud.io/summary/new_code?id=elrorro334_cine-back)

---

## 📑 Tabla de Contenidos
1. [Principios de Arquitectura y Reglas de Negocio](#-principios-de-arquitectura-y-reglas-de-negocio)
2. [Estructura del Proyecto (Clean Architecture)](#-estructura-del-proyecto-clean-architecture)
3. [Requisitos Previos](#-requisitos-previos)
4. [Configuración de Ambientes y Variables](#-configuración-de-ambientes-y-variables)
5. [Entity Framework Core: Migraciones, Rollback y BD desde Cero](#-entity-framework-core-migraciones-rollback-y-bd-desde-cero)
6. [Análisis Estático de Código con SonarCloud y GitHub Actions](#-análisis-estático-de-código-con-sonarcloud-y-github-actions)
7. [Suite de Pruebas Unitarias](#-suite-de-pruebas-unitarias)
8. [Ejecución de la API en Desarrollo](#-ejecución-de-la-api-en-desarrollo)
9. [Catálogo de Endpoints Principales](#-catálogo-de-endpoints-principales)

---

## 🏛 Principios de Arquitectura y Reglas de Negocio

### 1. Principio Fundamental de Autoridad (Cero Lógica en Next.js)
* **El Backend .NET Core es la única autoridad de negocio:** Todo cálculo contable, validación de stock, generación de folios (`VT-XXXXX`), asignación y bloqueo de butacas por función y validación de vigencia de pre-órdenes reside **exclusivamente** en este backend.
* El frontend Next.js opera como una capa de presentación pura (*Dumb UI / Presentation View*). Nunca se deben delegar al cliente cálculos de totales, estados de asientos o generación de números de ticket.
* **Persistencia Real en SQL Server (`cineDataBase`):**
  - Toda venta confirmada se persiste en las tablas `venta` y `detalle_venta`.
  - La ocupación de butacas se persiste en `estado_butaca_funcion` con `estato = true`.
  - Las existencias de dulcería se descuentan en `producto.stock_actual`.
  - Las promociones y cartelera se sirven dinámicamente desde los controladores REST.
* **Restricción de Íconos en Respuestas:** Los DTOs de salida no emiten emojis Unicode; retornan clases de íconos vectoriales normalizadas (ej. `fa-ticket`, `fa-clapperboard`, `fa-qrcode`).

---

## 📁 Estructura del Proyecto (Clean Architecture)

El backend está organizado en proyectos desacoplados (*Class Libraries*) con inversión de dependencias:

```
cine-back/
│
├── cine-back.slnx                     # Solución que orquesta los proyectos
│
├── cine-back.csproj                   # [Capa Presentación / API Host]
│   ├── Controllers/                   # Controladores REST (Auth, Cartelera, Taquilla, Dashboard)
│   ├── Program.cs                     # Pipeline HTTP, Inyección de Dependencias, CORS, JWT
│   ├── appsettings.json               # Configuración base
│   └── appsettings.Development.json   # Variables de entorno local (ignorado en Git)
│
├── Cine.Application/                  # [Capa de Aplicación - Class Library]
│   ├── Interfaces/                    # Contratos de servicios (IAuthService, ITaquillaService, ICineDbContext)
│   ├── Dtos/                          # Data Transfer Objects (Request/Response)
│   ├── Services/                      # Casos de uso y reglas de negocio
│   └── DependencyInjection.cs         # Contenedor IoC de aplicación
│
├── Cine.Domain/                       # [Capa de Dominio - Class Library]
│   └── Entities/                      # Entidades puras (Pelicula, Funcion, Sala, Butaca, Venta, etc.)
│
├── Cine.Infrastructure/               # [Capa de Infraestructura - Class Library]
│   ├── Persistence/                   # CineDbContext, mapeos Fluent API
│   │   └── Migrations/                # Historial de migraciones versionadas de EF Core
│   └── DependencyInjection.cs         # Registro de EF Core y SQL Server
│
└── cine-back.Tests/                   # [Capa de Pruebas - Proyecto xUnit]
    ├── Controllers/                   # Pruebas unitarias de controladores
    ├── Service/                       # Pruebas de reglas de negocio con Moq y FluentAssertions
    └── Integration/                   # Pruebas de integración
```

---

## 📋 Requisitos Previos

Antes de comenzar, asegúrate de tener instalado:

1. **[.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)** (v10.0.100 o superior).
2. **[Microsoft SQL Server 2022](https://www.microsoft.com/sql-server/)** (o LocalDB / SQL Server Express).
3. **[Java 17+ JDK](https://adoptium.net/)** (requerido únicamente si vas a ejecutar SonarScanner de forma local).
4. **Herramienta global de Entity Framework Core CLI:**
   ```bash
   dotnet tool install --global dotnet-ef
   ```
   *(Si ya la tienes instalada, puedes actualizarla con `dotnet tool update --global dotnet-ef`).*

---

## ⚙️ Configuración de Ambientes y Variables

El proyecto utiliza desacoplamiento total de secretos mediante `appsettings.Development.json` (excluido en `.gitignore` para no exponer credenciales sensibles).

1. Crea o verifica tu archivo `appsettings.Development.json` en la raíz del proyecto tomando como plantilla `appsettings.Development.json.example`:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "Microsoft.AspNetCore": "Information",
         "Microsoft.EntityFrameworkCore.Database.Command": "Information"
       }
     },
     "AllowedHosts": "*",
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=cineDataBase;Trusted_Connection=True;TrustServerCertificate=True;"
     },
     "Jwt": {
       "Key": "SuperSecretKeyParaEntornoLocalDeDesarrolloMinimo32Caracteres12345!",
       "Issuer": "CineTeatroXilotzinApi",
       "Audience": "CineTeatroXilotzinPWA"
     },
     "Cors": {
       "AllowedOrigins": [
         "http://localhost:3000",
         "https://localhost:3000",
         "http://localhost:5173",
         "https://localhost:5173"
       ]
     }
   }
   ```

2. **Parámetros Clave:**
   * `DefaultConnection`: Cadena de conexión hacia tu instancia de SQL Server. La base de datos estándar se denomina `cineDataBase`.
   * `Jwt:Key`: Llave simétrica de al menos 256 bits (32 caracteres) para el firmado de tokens de taquilla y clientes.
   * `Cors:AllowedOrigins`: Orígenes autorizados para comunicarse con la PWA en Next.js sin errores de Cross-Origin.

---

## 🗄️ Entity Framework Core: Migraciones, Rollback y BD desde Cero

Dado que el proyecto utiliza Clean Architecture, el `DbContext` reside en `Cine.Infrastructure` y el punto de arranque de configuración (`Program.cs` / `appsettings.json`) está en la raíz `cine-back`. Por ello, todos los comandos nativos de `dotnet-ef` especifican:
* `--project Cine.Infrastructure`
* `--startup-project .` (o `--startup-project cine-back.csproj`)

### 1. Historial de Migraciones Versionadas
El proyecto cuenta con las siguientes migraciones oficiales en `Cine.Infrastructure/Migrations`:
1. `20261022172515_M1_CreacionInicial`: Creación del esquema completo relacional (tablas de cartelera, salas, butacas, catálogo de dulcería, usuarios, ventas y auditoría).
2. `20261022172629_M2_AgregarColumnaCliente`: Evolución controlada de esquema sobre la entidad cliente.
3. `20261022172730_M3_AgregarIndiceCorreo`: Creación de índices de rendimiento y restricción de unicidad para correos electrónicos.

---

### 2. Comandos Nativos de Migración

#### A. Listar Migraciones y Estado Actual
Muestra las migraciones registradas en el código y cuáles han sido aplicadas a la base de datos:
```bash
dotnet ef migrations list --project Cine.Infrastructure --startup-project .
```
*(Para consultar las migraciones en código sin requerir conexión activa a la BD, añade `--no-connect`)*.

#### B. Crear una Nueva Migración
Si agregas nuevas entidades o modificas propiedades en `Cine.Domain` y `CineDbContext`:
```bash
dotnet ef migrations add NombreDeTuMigracion --project Cine.Infrastructure --startup-project .
```

#### C. Aplicar Migraciones Pendientes
Aplica todas las migraciones que falten por sincronizar en la base de datos `cineDataBase`:
```bash
dotnet ef database update --project Cine.Infrastructure --startup-project .
```

#### D. Verificar en SQL Server la Tabla de Auditoría
Puedes comprobar directamente en la base de datos el historial registrado por EF Core:
```sql
USE cineDataBase;
SELECT MigrationId, ProductVersion FROM [__EFMigrationsHistory];
```

---

### 3. Protocolo de Rollback (Reversión Controlada de Cambios)

Entity Framework Core permite revertir el esquema de forma segura hacia cualquier punto del historial mediante su método `Down()` sin alterar migraciones previas.

#### Escenario 1: Revertir a una Migración Específica (Rollback Parcial)
Para deshacer la migración `M3_AgregarIndiceCorreo` y regresar el esquema al estado de `M2_AgregarColumnaCliente`:
```bash
dotnet ef database update M2_AgregarColumnaCliente --project Cine.Infrastructure --startup-project .
```
* **Resultado:** EF Core ejecuta el método `Down()` de `M3_AgregarIndiceCorreo`, eliminando el índice de la base de datos y retirando el registro correspondiente de `[__EFMigrationsHistory]`.

#### Escenario 2: Reaplicar el Cambio Revertido
Para volver a avanzar a la última versión una vez corregido o validado el cambio:
```bash
dotnet ef database update --project Cine.Infrastructure --startup-project .
```
* **Resultado:** EF Core detecta que `M3_AgregarIndiceCorreo` está pendiente en código y vuelve a ejecutar su método `Up()`.

#### Escenario 3: Revertir Todas las Migraciones (Rollback Total a BD Vacía)
Para deshacer absolutamente todas las migraciones aplicadas y dejar la base de datos sin ninguna tabla de la aplicación:
```bash
dotnet ef database update 0 --project Cine.Infrastructure --startup-project .
```

#### Escenario 4: Eliminar una Migración de Código no Deseada
Si creaste una migración local con `migrations add` y aún no la aplicas (o ya le hiciste rollback con `database update`):
```bash
dotnet ef migrations remove --project Cine.Infrastructure --startup-project .
```

---

### 4. Inicialización de la Base de Datos Local Desde Cero (Clean Slate)

Cuando un nuevo desarrollador clona el repositorio o se requiere restablecer el ambiente de desarrollo desde cero aplicando la secuencia completa $M1 \rightarrow M2 \rightarrow M3$:

1. **(Opcional) Eliminar por completo la base de datos existente si se desea un reinicio limpio:**
   ```bash
   dotnet ef database drop --force --project Cine.Infrastructure --startup-project .
   ```

2. **Crear la base de datos y aplicar secuencialmente todas las migraciones:**
   ```bash
   dotnet ef database update --project Cine.Infrastructure --startup-project .
   ```

   > **¿Qué hace internamente EF Core en una base limpia?**
   > 1. Detecta que `cineDataBase` no existe y ejecuta `CREATE DATABASE [cineDataBase]`.
   > 2. Crea automáticamente la tabla de metadatos `[__EFMigrationsHistory]`.
   > 3. Ejecuta en estricto orden cronológico:
   >    - `20261022172515_M1_CreacionInicial` $\rightarrow$ Crea tablas, claves primarias, claves foráneas y relaciones relacionales.
   >    - `20261022172629_M2_AgregarColumnaCliente` $\rightarrow$ Aplica la evolución estructural del cliente.
   >    - `20261022172730_M3_AgregarIndiceCorreo` $\rightarrow$ Aplica el índice y restricción de unicidad.
   > 4. Registra los 3 identificadores en `[__EFMigrationsHistory]`, dejando la base lista para operar.

3. **Verificación rápida con `sqlcmd` (PowerShell o CMD):**
   ```powershell
   sqlcmd -S localhost -d cineDataBase -E -Q "SELECT MigrationId FROM __EFMigrationsHistory;"
   ```

---

### 5. Generación de Script SQL Idempotente (Para Despliegues en Producción / DBAs)
Para generar un script SQL completo que pueda ejecutarse en cualquier servidor SQL Server sin necesidad de la CLI de `dotnet-ef`:
```bash
dotnet ef migrations script --idempotent --project Cine.Infrastructure --startup-project . --output deploy_migrations.sql
```
*(El modificador `--idempotent` genera bloques `IF NOT EXISTS` verificando `__EFMigrationsHistory` antes de aplicar cada cambio, evitando errores si parte del esquema ya existe).*

---

## 🔍 Análisis Estático de Código con SonarCloud y GitHub Actions

El proyecto cuenta con un pipeline de calidad automatizado en **SonarCloud (SonarQube Cloud)** integrado mediante **GitHub Actions** en `.github/workflows/sonarcloud.yml`.

### 1. Métricas y Reglas del Quality Gate
Para que una contribución o Pull Request sea aprobado por el Quality Gate:
* **0** Bugs.
* **0** Vulnerabilidades de seguridad.
* **0** Security Hotspots sin revisar (100% revisados).
* **Cobertura de Pruebas Unitarias $\ge$ 80%** en código nuevo.
* **Deuda Técnica:** Grado **A** (duplicación < 3%, code smells controlados).

---

### 2. Configuración en GitHub Actions (`.github/workflows/sonarcloud.yml`)

El flujo se dispara automáticamente en:
* Cada `push` en cualquier rama (`branches: ['**']`).
* Creación o sincronización de `pull_request`.
* Ejecución manual vía `workflow_dispatch`.

#### Secretos Requeridos en el Repositorio de GitHub
Configurados en **Settings $\rightarrow$ Secrets and variables $\rightarrow$ Actions**:

| Nombre | Descripción |
| :--- | :--- |
| `SONAR_TOKEN` | Token de autenticación generado en tu cuenta de SonarCloud (*Account $\rightarrow$ Security $\rightarrow$ Generate Tokens*). |
| `GITHUB_TOKEN` | Token nativo provisto automáticamente por GitHub Actions para permisos de lectura y feedback en PRs. |

#### Variables Opcionales en GitHub Actions:
* `SONAR_PROJECT_KEY`: Clave del proyecto en SonarCloud (por defecto: `elrorro334_cine-back`).
* `SONAR_ORGANIZATION`: Organización en SonarCloud (por defecto: `elrorro334`).

#### Estructura del Workflow de CI:
```yaml
name: SonarQube Cloud Analysis

on:
  push:
    branches:
      - '**'
  pull_request:
    types: [opened, synchronize, reopened]
  workflow_dispatch:

permissions:
  contents: read
  pull-requests: read

jobs:
  sonarcloud:
    name: SonarQube Cloud Analysis
    runs-on: ubuntu-latest

    steps:
      - name: Descargar código del repositorio (historial completo)
        uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Configurar Java 17 (Requerido por SonarScanner)
        uses: actions/setup-java@v4
        with:
          distribution: 'temurin'
          java-version: '17'

      - name: Configurar SDK de .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Instalar SonarScanner para .NET
        run: dotnet tool install --global dotnet-sonarscanner

      - name: Compilar, Ejecutar Tests y Analizar con SonarCloud
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
          ConnectionStrings__DefaultConnection: "Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;"
          Jwt__Key: "ClaveDePruebaParaEntornoDeIntegracionContinuaEnGitHubActions123456"
          Jwt__Issuer: "CineTeatroXilotzinApi"
          Jwt__Audience: "CineTeatroXilotzinPWA"
        run: |
          export PATH="$PATH:$HOME/.dotnet/tools"
          
          dotnet-sonarscanner begin \
            /k:"${{ vars.SONAR_PROJECT_KEY || 'elrorro334_cine-back' }}" \
            /o:"${{ vars.SONAR_ORGANIZATION || 'elrorro334' }}" \
            /d:sonar.token="${{ secrets.SONAR_TOKEN }}" \
            /d:sonar.host.url="https://sonarcloud.io" \
            /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml" \
            /d:sonar.coverage.exclusions="**/Program.cs,**/Controllers/TestController.cs,**/Migrations/**" \
            /d:sonar.cpd.exclusions="**/Migrations/**" \
            /d:sonar.exclusions="**/Migrations/**"

          dotnet build --configuration Release

          dotnet test \
            --configuration Release \
            --no-build \
            --collect:"XPlat Code Coverage;Format=opencover"

          dotnet-sonarscanner end /d:sonar.token="${{ secrets.SONAR_TOKEN }}"
```

---

### 3. Exclusiones de Análisis Configuradas
Para evitar falsos positivos y métricas distorsionadas en código generado automáticamente o archivos de arranque:
1. **En los parámetros de SonarScanner:**
   - `sonar.coverage.exclusions`: Se excluyen `Program.cs`, controladores de prueba de ping y la carpeta de migraciones `**/Migrations/**`.
   - `sonar.cpd.exclusions` y `sonar.exclusions`: Se excluyen los archivos autogenerados de EF Core en `**/Migrations/**`.
2. **En el archivo de proyecto `Cine.Infrastructure.csproj`:**
   ```xml
   <ItemGroup>
     <Compile Update="Migrations\**">
       <SonarQubeExclude>true</SonarQubeExclude>
     </Compile>
   </ItemGroup>
   ```

---

### 4. Ejecución Local de SonarCloud

Para ejecutar el análisis en tu máquina local antes de enviar un commit a GitHub:

1. **Instalar la herramienta SonarScanner:**
   ```bash
   dotnet tool install --global dotnet-sonarscanner
   ```

2. **Ejecutar el ciclo completo de análisis:**

   **En PowerShell (Windows):**
   ```powershell
   dotnet-sonarscanner begin `
     /k:"elrorro334_cine-back" `
     /o:"elrorro334" `
     /d:sonar.token="TU_SONAR_TOKEN" `
     /d:sonar.host.url="https://sonarcloud.io" `
     /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml" `
     /d:sonar.coverage.exclusions="**/Program.cs,**/Controllers/TestController.cs,**/Migrations/**" `
     /d:sonar.cpd.exclusions="**/Migrations/**" `
     /d:sonar.exclusions="**/Migrations/**"

   dotnet build --configuration Release

   dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage;Format=opencover"

   dotnet-sonarscanner end /d:sonar.token="TU_SONAR_TOKEN"
   ```

   **En Bash (Linux / macOS):**
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

   dotnet build --configuration Release

   dotnet test --configuration Release --no-build --collect:"XPlat Code Coverage;Format=opencover"

   dotnet-sonarscanner end /d:sonar.token="TU_SONAR_TOKEN"
   ```

---

## 🧪 Suite de Pruebas Unitarias

El proyecto cuenta con un conjunto exhaustivo de pruebas en `cine-back.Tests` utilizando:
* **xUnit** (v2.9.3) como framework de pruebas.
* **Moq** (v4.21.0) para aislamiento de dependencias (`ICineDbContext`, `HttpMessageHandler`).
* **FluentAssertions** (v8.11.0) para aserciones expresivas.
* **Coverlet.collector** (v6.0.4) para recolección de cobertura de código en formato OpenCover.

### Ejecución de Pruebas:
```bash
# Ejecutar todas las pruebas unitarias
dotnet test

# Ejecutar pruebas recolectando cobertura de código
dotnet test --collect:"XPlat Code Coverage;Format=opencover"
```

*Actualmente la suite cuenta con **67 pruebas unitarias** pasando al 100% sin errores, cubriendo escenarios exitosos, validaciones de seguridad, cálculo de cortes de caja, manejo de excepciones y fallas de red.*

---

## 🚀 Ejecución de la API en Desarrollo

1. **Restaurar paquetes y compilar:**
   ```bash
   dotnet restore
   dotnet build
   ```

2. **Asegurar que la base de datos local esté actualizada:**
   ```bash
   dotnet ef database update --project Cine.Infrastructure --startup-project .
   ```

3. **Iniciar la aplicación:**
   ```bash
   dotnet run --project cine-back.csproj
   ```

4. **Acceder a la Documentación Interactiva de Swagger:**
   * URL de desarrollo: `http://localhost:5000` o `https://localhost:7088` (Swagger redirigido a la raíz en entorno Development).

---

## 📡 Catálogo de Endpoints Principales

| Módulo | Método | Endpoint | Descripción | Auth Requerida |
| :--- | :---: | :--- | :--- | :---: |
| **Auth** | `POST` | `/api/auth/login` | Autenticación de vendedores con verificación estricta de IP local | No |
| **Auth** | `GET` | `/api/auth/check-network` | Valida si la IP remota pertenece al perímetro autorizado del cine | No |
| **Cartelera** | `GET` | `/api/cartelera/activas` | Lista de películas activas en cartelera y sus funciones programadas | No |
| **Taquilla** | `POST` | `/api/taquilla/registrar-pelicula` | Registra una película vinculándola y enriqueciéndola con la API de TMDB | Sí (Admin/Taquilla) |
| **Taquilla** | `DELETE`| `/api/taquilla/pelicula/{id}` | Desactiva o da de baja una película de la cartelera | Sí (Admin) |
| **Dashboard**| `GET` | `/api/dashboard/resumen` | 4 KPIs gerenciales (Ingresos, Taquilla, Dulcería, Efectivo vs Tarjeta) | Sí (Admin) |
| **Dashboard**| `GET` | `/api/dashboard/semana-actual` | Fechas límites de la semana cinematográfica actual (Jueves a Miércoles) | Sí (Admin) |
| **Dashboard**| `GET` | `/api/dashboard/exportar-transacciones` | Genera y descarga el archivo CSV con UTF-8 BOM para Excel / Contabilidad | Sí (Admin) |

---

## 📄 Licencia y Contribución
Proyecto desarrollado para la gestión operativa y taquilla del **Cine Teatro Xilotzin**. Todos los derechos reservados.
