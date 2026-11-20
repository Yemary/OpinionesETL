using System.Data;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesETL.Core.Interfaces;
using OpinionesETL.Infrastructure.Configuracion;

namespace OpinionesETL.Infrastructure.Staging;

/// <summary>
/// Guarda los registros extraídos en stg.&lt;NombreDelModelo&gt; usando SqlBulkCopy (carga masiva).
/// Cada ejecución reemplaza el contenido anterior, así repetir la extracción no duplica datos.
/// </summary>
public class SqlStagingWriter<T> : IStagingWriter<T>
{
    private static readonly PropertyInfo[] Propiedades =
        typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

    private readonly string _cadenaConexion;
    private readonly ILogger<SqlStagingWriter<T>> _logger;

    public SqlStagingWriter(IOptions<StagingOptions> opciones, ILogger<SqlStagingWriter<T>> logger)
    {
        _cadenaConexion = opciones.Value.CadenaConexion;
        _logger = logger;
    }

    public async Task<int> GuardarAsync(IReadOnlyList<T> registros, CancellationToken cancellationToken)
    {
        var reloj = Stopwatch.StartNew();
        string tabla = $"stg.{typeof(T).Name}";

        var datos = new DataTable();
        foreach (var p in Propiedades) datos.Columns.Add(p.Name, typeof(string));

        foreach (var registro in registros)
        {
            var fila = datos.NewRow();
            foreach (var p in Propiedades)
                fila[p.Name] = (object?)p.GetValue(registro)?.ToString() ?? DBNull.Value;
            datos.Rows.Add(fila);
        }

        try
        {
            await using var conexion = new SqlConnection(_cadenaConexion);
            await conexion.OpenAsync(cancellationToken);
            using var transaccion = conexion.BeginTransaction();

            try
            {
                await using (var vaciar = new SqlCommand($"TRUNCATE TABLE {tabla}", conexion, transaccion))
                    await vaciar.ExecuteNonQueryAsync(cancellationToken);

                using var bulk = new SqlBulkCopy(conexion, SqlBulkCopyOptions.Default, transaccion)
                {
                    DestinationTableName = tabla,
                    BatchSize = 1000
                };
                foreach (var p in Propiedades) bulk.ColumnMappings.Add(p.Name, p.Name);

                await bulk.WriteToServerAsync(datos, cancellationToken);
                transaccion.Commit();
            }
            catch
            {
                transaccion.Rollback();
                throw;
            }
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Error al guardar en staging ({Tabla})", tabla);
            throw;
        }

        _logger.LogInformation("Staging {Tabla}: {Cantidad} registros guardados en {Ms} ms",
            tabla, datos.Rows.Count, reloj.ElapsedMilliseconds);
        return datos.Rows.Count;
    }
}