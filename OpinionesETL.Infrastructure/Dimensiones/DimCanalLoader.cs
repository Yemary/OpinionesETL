using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesETL.Infrastructure.Configuracion;

namespace OpinionesETL.Infrastructure.Dimensiones;

public class DimCanalLoader : DimensionLoaderBase
{
    public DimCanalLoader(IOptions<DataWarehouseOptions> opciones, ILogger<DimCanalLoader> logger)
        : base(opciones, logger) { }

    public override string Nombre => "DimCanal";

    protected override string TablaTemporal =>
        @"CREATE TABLE #src (
            Canal      NVARCHAR(20) NOT NULL,
            Plataforma NVARCHAR(50) NOT NULL);";

    protected override string SqlMerge =>
        @"MERGE dbo.DimCanal AS t
          USING #src AS s ON t.Canal = s.Canal AND t.Plataforma = s.Plataforma
          WHEN NOT MATCHED BY TARGET
              THEN INSERT (Canal, Plataforma) VALUES (s.Canal, s.Plataforma)
          OUTPUT $action;";

    protected override Task<DataTable> LeerOrigenAsync(CancellationToken ct) =>
        LeerSqlAsync(
            @"SELECT N'Encuesta' AS Canal, N'No aplica' AS Plataforma
              UNION ALL SELECT N'Web', N'No aplica'
              UNION ALL SELECT N'Red Social', CAST(NombrePlataforma AS NVARCHAR(50))
                        FROM dbo.Plataforma_Social", ct);
}