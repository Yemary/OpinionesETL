using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesETL.Infrastructure.Configuracion;

namespace OpinionesETL.Infrastructure.Dimensiones;

public class DimClasificacionLoader : DimensionLoaderBase
{
    public DimClasificacionLoader(IOptions<DataWarehouseOptions> opciones, ILogger<DimClasificacionLoader> logger)
        : base(opciones, logger) { }

    public override string Nombre => "DimClasificacion";

    protected override string TablaTemporal =>
        @"CREATE TABLE #src (
            IdClasificacion TINYINT      NOT NULL,
            Clasificacion   NVARCHAR(20) NOT NULL);";

    protected override string SqlMerge =>
        @"MERGE dbo.DimClasificacion AS t
          USING #src AS s ON t.IdClasificacion = s.IdClasificacion
          WHEN MATCHED AND t.Clasificacion <> s.Clasificacion
              THEN UPDATE SET Clasificacion = s.Clasificacion
          WHEN NOT MATCHED BY TARGET
              THEN INSERT (IdClasificacion, Clasificacion) VALUES (s.IdClasificacion, s.Clasificacion)
          OUTPUT $action;";

    protected override Task<DataTable> LeerOrigenAsync(CancellationToken ct) =>
        LeerSqlAsync(
            @"SELECT CAST(IdClasificacion AS TINYINT) AS IdClasificacion,
                     CAST(NombreClasificacion AS NVARCHAR(20)) AS Clasificacion
              FROM dbo.Clasificacion
              WHERE IdClasificacion <> 0
              UNION ALL
              SELECT CAST(0 AS TINYINT), N'Sin clasificar'", ct);
}