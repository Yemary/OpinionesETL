using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesETL.Core.Interfaces;
using OpinionesETL.Core.Modelos;
using OpinionesETL.Infrastructure.Configuracion;

namespace OpinionesETL.Infrastructure.Dimensiones;

public abstract class DimensionLoaderBase : IDimensionLoader
{
    private readonly ILogger _logger;
    protected DataWarehouseOptions Opciones { get; }

    protected DimensionLoaderBase(IOptions<DataWarehouseOptions> opciones, ILogger logger)
    {
        Opciones = opciones.Value;
        _logger = logger;
    }

    public abstract string Nombre { get; }
    protected abstract string TablaTemporal { get; }
    protected abstract string SqlMerge { get; }
    protected abstract Task<DataTable> LeerOrigenAsync(CancellationToken ct);

    protected virtual Task PrepararAsync(SqlConnection dw, SqlTransaction tx, CancellationToken ct)
        => Task.CompletedTask;

    public async Task<ResultadoDimension> CargarAsync(CancellationToken ct)
    {
        var reloj = Stopwatch.StartNew();
        _logger.LogInformation("Cargando {Dimension}...", Nombre);

        try
        {
            var origen = await LeerOrigenAsync(ct);
            int insertados = 0, actualizados = 0;

            await using var dw = new SqlConnection(Opciones.CadenaConexionDW);
            await dw.OpenAsync(ct);
            await using var tx = (SqlTransaction)await dw.BeginTransactionAsync(ct);

            await PrepararAsync(dw, tx, ct);

            await using (var crear = new SqlCommand(TablaTemporal, dw, tx))
                await crear.ExecuteNonQueryAsync(ct);

            using (var bulk = new SqlBulkCopy(dw, SqlBulkCopyOptions.Default, tx))
            {
                bulk.DestinationTableName = "#src";
                bulk.BatchSize = 5000;
                foreach (DataColumn c in origen.Columns)
                    bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);
                await bulk.WriteToServerAsync(origen, ct);
            }

            await using (var merge = new SqlCommand(SqlMerge, dw, tx))
            await using (var lector = await merge.ExecuteReaderAsync(ct))
            {
                while (await lector.ReadAsync(ct))
                {
                    if (lector.GetString(0) == "INSERT") insertados++;
                    else actualizados++;
                }
            }

            var resultado = new ResultadoDimension(Nombre, origen.Rows.Count, insertados, actualizados,
                reloj.ElapsedMilliseconds);

            await using (var log = new SqlCommand(
                "INSERT INTO dbo.LogCargaDimension (Dimension, Leidos, Insertados, Actualizados, DuracionMs) " +
                "VALUES (@d, @l, @i, @a, @m)", dw, tx))
            {
                log.Parameters.AddWithValue("@d", resultado.Dimension);
                log.Parameters.AddWithValue("@l", resultado.Leidos);
                log.Parameters.AddWithValue("@i", resultado.Insertados);
                log.Parameters.AddWithValue("@a", resultado.Actualizados);
                log.Parameters.AddWithValue("@m", (int)resultado.DuracionMs);
                await log.ExecuteNonQueryAsync(ct);
            }

            await tx.CommitAsync(ct);

            _logger.LogInformation("{Dimension}: leídos {Leidos} | nuevos {Nuevos} | actualizados {Act} | {Ms} ms",
                resultado.Dimension, resultado.Leidos, resultado.Insertados, resultado.Actualizados,
                resultado.DuracionMs);
            return resultado;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error al cargar {Dimension}", Nombre);
            throw;
        }
    }

    protected async Task<DataTable> LeerSqlAsync(string sql, CancellationToken ct)
    {
        var tabla = new DataTable();
        await using var conexion = new SqlConnection(Opciones.CadenaConexionOrigen);
        await conexion.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conexion);
        await using var lector = await cmd.ExecuteReaderAsync(ct);
        tabla.Load(lector);
        return tabla;
    }
}