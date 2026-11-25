using System.Diagnostics;
using OpinionesETL.Core.Interfaces;
using OpinionesETL.Core.Modelos;

namespace OpinionesETL.CargaDW;

public class DimensionWorker : BackgroundService
{
    private readonly IEnumerable<IDimensionLoader> _cargadores;
    private readonly IHostApplicationLifetime _ciclo;
    private readonly ILogger<DimensionWorker> _logger;

    public DimensionWorker(
        IEnumerable<IDimensionLoader> cargadores,
        IHostApplicationLifetime ciclo,
        ILogger<DimensionWorker> logger)
    {
        _cargadores = cargadores;
        _ciclo = ciclo;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reloj = Stopwatch.StartNew();
        _logger.LogInformation("=== INICIO DE LA CARGA DE DIMENSIONES ===");

        var salidas = await Task.WhenAll(_cargadores.Select(c => EjecutarAsync(c, stoppingToken)));

        _logger.LogInformation("=== RESUMEN ===");
        foreach (var s in salidas)
        {
            if (s.Resultado is { } r)
                _logger.LogInformation("{Dim}: OK | leídos {L} | nuevos {N} | actualizados {A} | {Ms} ms",
                    r.Dimension, r.Leidos, r.Insertados, r.Actualizados, r.DuracionMs);
            else
                _logger.LogInformation("{Dim}: ERROR", s.Dimension);
        }

        _logger.LogInformation("=== FIN: {Ok} de {Total} dimensiones cargadas correctamente en {Seg:F1} s ===",
            salidas.Count(s => s.Resultado != null), salidas.Length, reloj.Elapsed.TotalSeconds);

        _ciclo.StopApplication();
    }

    private async Task<Salida> EjecutarAsync(IDimensionLoader cargador, CancellationToken ct)
    {
        try
        {
            return new Salida(cargador.Nombre, await cargador.CargarAsync(ct));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError("No se pudo cargar {Dim}: {Mensaje}", cargador.Nombre, ex.Message);
            return new Salida(cargador.Nombre, null);
        }
    }

    private record Salida(string Dimension, ResultadoDimension? Resultado);
}