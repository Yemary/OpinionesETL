using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesETL.Infrastructure.Configuracion;

namespace OpinionesETL.Infrastructure.Dimensiones;

public class DimClienteLoader : DimensionLoaderBase
{
    public DimClienteLoader(IOptions<DataWarehouseOptions> opciones, ILogger<DimClienteLoader> logger)
        : base(opciones, logger) { }

    public override string Nombre => "DimCliente";

    protected override string TablaTemporal =>
        @"CREATE TABLE #src (
            IdClienteOrigen INT           NOT NULL,
            NombreCliente   NVARCHAR(100) NOT NULL,
            Email           NVARCHAR(150) NULL);";

    protected override string SqlMerge =>
        @"MERGE dbo.DimCliente AS t
          USING #src AS s ON t.IdClienteOrigen = s.IdClienteOrigen
          WHEN MATCHED AND EXISTS (
                 SELECT s.NombreCliente, s.Email
                 EXCEPT
                 SELECT t.NombreCliente, t.Email)
              THEN UPDATE SET NombreCliente = s.NombreCliente, Email = s.Email
          WHEN NOT MATCHED BY TARGET
              THEN INSERT (IdClienteOrigen, NombreCliente, Email)
                   VALUES (s.IdClienteOrigen, s.NombreCliente, s.Email)
          OUTPUT $action;";

    protected override async Task PrepararAsync(SqlConnection dw, SqlTransaction tx, CancellationToken ct)
    {
        const string sql =
            @"IF NOT EXISTS (SELECT 1 FROM dbo.DimCliente WHERE IdClienteDW = 0)
              BEGIN
                  SET IDENTITY_INSERT dbo.DimCliente ON;
                  INSERT INTO dbo.DimCliente (IdClienteDW, IdClienteOrigen, NombreCliente, Email)
                  VALUES (0, NULL, N'Anónimo', NULL);
                  SET IDENTITY_INSERT dbo.DimCliente OFF;
              END";
        await using var cmd = new SqlCommand(sql, dw, tx);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    protected override Task<DataTable> LeerOrigenAsync(CancellationToken ct) =>
        LeerSqlAsync(
            @"SELECT IdCliente AS IdClienteOrigen, Nombre AS NombreCliente, Email
              FROM dbo.Cliente", ct);
}