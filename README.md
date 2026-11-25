# OpinionesETL – Proceso de extracción (E de ETL)

**Autor:** Yemary De La Cruz Soto 2025-1244

**Fecha:** 20-11-2026

---

## 1. Descripción

Este proyecto es la etapa de **extracción (E de ETL)** del *Sistema de Análisis de Opiniones de Clientes*. Un **Worker Service en .NET 8** extrae opiniones de tres fuentes distintas y las guarda, sin transformarlas, en tablas de staging dentro de SQL Server.

La arquitectura sigue los principios de **Clean Architecture**, con tres capas y dependencias que apuntan hacia el núcleo.

**Alcance:** este repositorio cubre solo la extracción y el guardado en staging. La transformación, la carga a la base analítica y el dashboard corresponden a otras etapas del proyecto.

## 2. Fuentes de datos

| Fuente | Tipo | Tecnología | Clase | Registros |
|---|---|---|---|---|
| Encuestas internas (`surveys_part1.csv`) | Archivo CSV | CsvHelper | `CsvExtractor` | 500 |
| Reseñas web (`SitioWebDB`) | Base de datos relacional | ADO.NET (`Microsoft.Data.SqlClient`) | `DatabaseExtractor` | 200 |
| Comentarios de redes sociales | API REST | `HttpClient` / `IHttpClientFactory` | `ApiExtractor` | 200 |

> `SitioWebDB` simula la base de datos del sitio web. Se carga con `Scripts/SitioWebDB_Fuente.sql` a partir de las reseñas de `web_reviews.csv`.
> La API (`OpinionesETL.FuenteApi`) simula el servicio de redes sociales y publica el contenido de `social_comments.csv`.

## 3. Arquitectura

### Estructura de la solución

```
OpinionesETL
├── OpinionesETL.Core             Núcleo: modelos e interfaces (no depende de nadie)
├── OpinionesETL.Infrastructure   Extractores, escritor de staging y configuración
├── OpinionesETL.Worker           Servicio que orquesta el proceso
├── OpinionesETL.FuenteApi        API que simula la fuente de comentarios sociales
├── Scripts                       Scripts SQL (SitioWebDB y StagingDB)
└── Diagramas                     Diagrama de arquitectura y diagrama de flujo
```

### Capas y responsabilidades

| Capa | Contenido | Responsabilidad |
|---|---|---|
| **Core** | `IExtractor<T>`, `IStagingWriter<T>`, `EncuestaRaw`, `ResenaRaw`, `ComentarioRaw` | Define los contratos y los modelos. No depende de ninguna otra capa ni de ninguna tecnología externa |
| **Infrastructure** | `CsvExtractor`, `DatabaseExtractor`, `ApiExtractor`, `SqlStagingWriter<T>`, clases de opciones | Implementa los contratos con tecnologías concretas |
| **Worker** | `ExtractionWorker`, `Program.cs` | Orquesta el proceso, registra la inyección de dependencias y los logs |

Regla de dependencias: `Worker → Infrastructure → Core`.

### Diagramas
Están en la carpeta `Diagramas`:
- `Diagrama_Arquitectura_ETL` (componentes y capas).
- `Diagrama_Flujo_ETL` (flujo del proceso de extracción).

## 4. Flujo del proceso

1. El Worker carga la configuración desde `appsettings.json`.
2. Registra el inicio del proceso con `ILogger`.
3. Lanza las tres extracciones **en paralelo** con `Task.WhenAll`.
4. Cada fuente se procesa de forma aislada: extrae y luego guarda en staging.
5. Si una fuente falla, se registra el error y las otras dos continúan.
6. Al terminar, el Worker muestra un resumen con el estado, los registros extraídos y guardados de cada fuente y el tiempo total.
7. El proceso se detiene solo (`StopApplication`).

## 5. Staging

Base de datos: `StagingDB`, esquema `stg`.

| Tabla | Origen |
|---|---|
| `stg.EncuestaRaw` | CSV de encuestas |
| `stg.ResenaRaw` | Base de datos relacional |
| `stg.ComentarioRaw` | API REST |

Decisiones de diseño:
- **Todas las columnas son texto y aceptan nulos.** La extracción no valida ni rechaza datos; esa responsabilidad corresponde a la etapa de transformación.
- **Cada ejecución reemplaza el contenido anterior** (`TRUNCATE` dentro de una transacción), así repetir el proceso no genera duplicados.
- La columna `FechaExtraccion` registra cuándo se extrajo cada fila.
- El guardado usa **`SqlBulkCopy`** para cargar grandes volúmenes de forma eficiente.

## 6. Atributos de calidad

| Atributo | Cómo se cumple |
|---|---|
| **Rendimiento** | Código asíncrono (`async/await`), extracción de las tres fuentes en paralelo (`Task.WhenAll`), carga masiva con `SqlBulkCopy` y tiempos de ejecución medidos con `Stopwatch` y registrados en los logs |
| **Escalabilidad** | Todas las fuentes cumplen la interfaz `IExtractor<T>`. Agregar una fuente nueva implica crear una clase y registrarla, sin modificar el Worker. El escritor de staging es genérico. Rutas y conexiones están en la configuración |
| **Seguridad** | La conexión a SQL Server usa autenticación de Windows, sin usuario ni contraseña en los archivos. La API exige una clave en el encabezado `X-Api-Key` y responde `401` sin ella. Las consultas del escritor usan solo nombres de tabla definidos por el código |
| **Mantenibilidad** | Capas separadas, un solo motivo de cambio por clase (SOLID), inyección de dependencias, configuración tipada con `IOptions<T>` y registro de eventos y errores con `ILogger` |
| **Confiabilidad** | Aislamiento de fallos por fuente, transacción en el guardado y tiempo máximo de espera en las llamadas a la API |

## 6.1 Paquetes utilizados (proyecto Infrastructure)

| Paquete | Versión |
|---|---|
| CsvHelper | 33.1.0 |
| Microsoft.Data.SqlClient | 7.1.2 |
| Microsoft.Extensions.Http | 8.0.1 |
| Microsoft.Extensions.Options | 8.0.2 |
| Microsoft.Extensions.Logging.Abstractions | 8.0.x |

## 7. Requisitos

- Visual Studio 2022 con la carga de trabajo de ASP.NET y desarrollo web.
- .NET 8 SDK.
- SQL Server y SQL Server Management Studio (SSMS).
- Los archivos `surveys_part1.csv` y `social_comments.csv`. **No están incluidos en el repositorio**, por lo que quien lo clone debe colocarlos en una carpeta propia.

## 8. Cómo ejecutarlo

### Paso 1: crear las bases de datos
En SSMS, abre y ejecuta, en este orden:
1. `Scripts/SitioWebDB_Fuente.sql`. Debe mostrar `TotalResenas = 200`.
2. `Scripts/StagingDB_Staging.sql`. Debe crear las tres tablas `stg.*`.

### Paso 2: ajustar la configuración
**`OpinionesETL.Worker/appsettings.json`**

| Clave | Qué poner |
|---|---|
| `Fuentes:Csv:Carpeta` | Carpeta donde está `surveys_part1.csv` |
| `Fuentes:BaseDatos:CadenaConexion` | Conexión a `SitioWebDB` |
| `Fuentes:Api:UrlBase` | `http://localhost:5100` |
| `Fuentes:Api:ApiKey` | La misma clave que usa la API |
| `Staging:CadenaConexion` | Conexión a `StagingDB` |

**`OpinionesETL.FuenteApi/appsettings.json`**

| Clave | Qué poner |
|---|---|
| `RutaCsv` | Ruta completa de `social_comments.csv` |
| `ApiKey` | La clave de acceso |

En JSON, cada barra invertida de una ruta de Windows se escribe doble (`\\`).

### Paso 3: iniciar la API
Clic derecho sobre `OpinionesETL.FuenteApi` → **Establecer como proyecto de inicio** → **Ctrl+F5**.

Comprobar en el navegador: `http://localhost:5100/api/salud` debe responder `{"estado":"activo"}`.

### Paso 4: ejecutar el Worker
Con la API encendida: clic derecho sobre `OpinionesETL.Worker` → **Establecer como proyecto de inicio** → **Ctrl+F5**.

### Paso 5: verificar en StagingDB

```sql
SELECT 'stg.EncuestaRaw'  AS Tabla, COUNT(*) AS Registros FROM stg.EncuestaRaw
UNION ALL SELECT 'stg.ResenaRaw',     COUNT(*) FROM stg.ResenaRaw
UNION ALL SELECT 'stg.ComentarioRaw', COUNT(*) FROM stg.ComentarioRaw;
```

Resultado esperado: **500, 200 y 200**.

## 9. Resultado esperado en consola

```
=== RESUMEN ===
CSV (encuestas): OK | extraídos 500 | guardados 500
Base de datos relacional (reseñas web): OK | extraídos 200 | guardados 200
API REST (comentarios sociales): OK | extraídos 200 | guardados 200
=== FIN: 3 de 3 fuentes correctas ===
```

## 10. Pruebas realizadas

| Prueba | Procedimiento | Resultado |
|---|---|---|
| Ejecución normal | Worker con la API encendida | 3 de 3 fuentes correctas; 500, 200 y 200 registros |
| Resiliencia | Worker con la API apagada | Falla solo la API; las otras dos fuentes terminan bien y el staging conserva sus datos (`2 de 3 fuentes correctas`) |
| Repetición | Worker ejecutado dos veces seguidas | Los conteos se mantienen en 500, 200 y 200, sin duplicados |
| Seguridad de la API | Petición a `/api/comentarios` sin clave | Respuesta `401` |

## 11. Limitaciones y trabajo futuro

- La clave de la API está en `appsettings.json` por ser un entorno de demostración. En producción debe guardarse en variables de entorno, User Secrets o un almacén de secretos.
- La API usa HTTP sin cifrado, porque se ejecuta de forma local. En producción debe usar HTTPS.
- La extracción es completa en cada ejecución. Una mejora posible es la extracción incremental, que traiga solo los datos nuevos.
- No hay reintentos automáticos cuando una fuente falla.
- Etapas pendientes: transformación y validación, carga a la base analítica y dashboard.

## 12. Tecnologías

C# · .NET 8 · Worker Service · ASP.NET Core Minimal API · CsvHelper · ADO.NET · HttpClient · SQL Server · ILogger · Clean Architecture

## Carga de dimensiones del DataWarehouse (OpinionesETL.CargaDW)

Worker Service que carga las dimensiones de la base analítica `OpinionesDW` desde la base transaccional `OpinionesClientesDB`.

| Dimensión | Origen |
|---|---|
| `DimProducto` | `Producto` y `Categoria` |
| `DimCliente` | `Cliente` (más el registro «Anónimo») |
| `DimCanal` | Encuesta, Web y `Plataforma_Social` |
| `DimClasificacion` | `Clasificacion` (más «Sin clasificar») |
| `DimFecha` | Calendario generado por código (2020 a 2030) |

### Cómo funciona
1. Cada dimensión tiene su propia clase (`IDimensionLoader`) en `OpinionesETL.Infrastructure/Dimensiones`.
2. Se leen los datos de origen y se cargan a una tabla temporal con `SqlBulkCopy`.
3. Un `MERGE` inserta los registros nuevos, actualiza solo los que cambiaron y no duplica nada.
4. Cada carga se hace dentro de una transacción y queda registrada en `dbo.LogCargaDimension`.
5. Las cinco dimensiones se cargan en paralelo y, si una falla, las demás continúan.

### Cómo ejecutarlo
1. Ejecutar `Scripts/OpinionesDW_LogCarga.sql` en `OpinionesDW`.
2. Ajustar las conexiones en `OpinionesETL.CargaDW/appsettings.json`.
3. Establecer `OpinionesETL.CargaDW` como proyecto de inicio y ejecutar con **Ctrl+F5**.

### Resultado esperado
```
DimClasificacion: OK | leídos 4 | nuevos 4 | actualizados 0
DimCanal: OK | leídos 5 | nuevos 5 | actualizados 0
DimFecha: OK | leídos 4018 | nuevos 4018 | actualizados 0
DimProducto: OK | leídos 200 | nuevos 200 | actualizados 0
DimCliente: OK | leídos 500 | nuevos 500 | actualizados 0
=== FIN: 5 de 5 dimensiones cargadas correctamente ===
```
