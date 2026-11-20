using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesETL.Core.Interfaces;
using OpinionesETL.Core.Modelos;
using OpinionesETL.Infrastructure.Configuracion;

namespace OpinionesETL.Infrastructure.Extractores;

public class DatabaseExtractor : IExtractor<ResenaRaw>
{
    private readonly BaseDatosOptions _opciones;
    private readonly ILogger<DatabaseExtractor> _logger;

    public DatabaseExtractor(IOptions<FuentesOptions> opciones, ILogger<DatabaseExtractor> logger)
    {
        _opciones = opciones.Value.BaseDatos;
        _logger = logger;
    }

    public string Nombre => "Base de datos relacional (reseñas web)";

    public async Task<IReadOnlyList<ResenaRaw>> ExtraerAsync(CancellationToken cancellationToken)
    {
        var reloj = Stopwatch.StartNew();
        _logger.LogInformation("Iniciando extracción desde la base de datos relacional");

        var registros = new List<ResenaRaw>();
        try
        {
            await using var conexion = new SqlConnection(_opciones.CadenaConexion);
            await conexion.OpenAsync(cancellationToken);

            await using var comando = new SqlCommand(_opciones.Consulta, conexion);
            await using var lector = await comando.ExecuteReaderAsync(cancellationToken);

            while (await lector.ReadAsync(cancellationToken))
            {
                registros.Add(new ResenaRaw
                {
                    IdReview = Leer(lector, 0),
                    IdCliente = Leer(lector, 1),
                    IdProducto = Leer(lector, 2),
                    Fecha = Leer(lector, 3),
                    Comentario = Leer(lector, 4),
                    Rating = Leer(lector, 5)
                });
            }
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Error al extraer desde la base de datos relacional");
            throw;
        }

        _logger.LogInformation("Extracción de base de datos completada: {Cantidad} registros en {Ms} ms",
            registros.Count, reloj.ElapsedMilliseconds);
        return registros;
    }

    private static string Leer(SqlDataReader lector, int indice) =>
        lector.IsDBNull(indice) ? string.Empty : lector.GetValue(indice)?.ToString() ?? string.Empty;
}