using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesETL.Infrastructure.Configuracion;

namespace OpinionesETL.Infrastructure.Dimensiones;

public class DimProductoLoader : DimensionLoaderBase
{
    public DimProductoLoader(IOptions<DataWarehouseOptions> opciones, ILogger<DimProductoLoader> logger)
        : base(opciones, logger) { }

    public override string Nombre => "DimProducto";

    protected override string TablaTemporal =>
        @"CREATE TABLE #src (
            IdProductoOrigen  INT           NOT NULL,
            NombreProducto    NVARCHAR(100) NOT NULL,
            IdCategoriaOrigen INT           NOT NULL,
            Categoria         NVARCHAR(100) NOT NULL);";

    protected override string SqlMerge =>
        @"MERGE dbo.DimProducto AS t
          USING #src AS s ON t.IdProductoOrigen = s.IdProductoOrigen
          WHEN MATCHED AND EXISTS (
                 SELECT s.NombreProducto, s.IdCategoriaOrigen, s.Categoria
                 EXCEPT
                 SELECT t.NombreProducto, t.IdCategoriaOrigen, t.Categoria)
              THEN UPDATE SET NombreProducto = s.NombreProducto,
                              IdCategoriaOrigen = s.IdCategoriaOrigen,
                              Categoria = s.Categoria
          WHEN NOT MATCHED BY TARGET
              THEN INSERT (IdProductoOrigen, NombreProducto, IdCategoriaOrigen, Categoria)
                   VALUES (s.IdProductoOrigen, s.NombreProducto, s.IdCategoriaOrigen, s.Categoria)
          OUTPUT $action;";

    protected override Task<DataTable> LeerOrigenAsync(CancellationToken ct) =>
        LeerSqlAsync(
            @"SELECT p.IdProducto AS IdProductoOrigen, p.Nombre AS NombreProducto,
                     p.IdCategoria AS IdCategoriaOrigen, c.NombreCategoria AS Categoria
              FROM dbo.Producto p
              INNER JOIN dbo.Categoria c ON c.IdCategoria = p.IdCategoria", ct);
}