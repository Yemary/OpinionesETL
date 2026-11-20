using System.Diagnostics;
using OpinionesETL.Core.Interfaces;
using OpinionesETL.Core.Modelos;

namespace OpinionesETL.Worker;

public class ExtractionWorker : BackgroundService
{
    private readonly IExtractor<EncuestaRaw> _extractorEncuestas;
    private readonly IExtractor<ResenaRaw> _extractorResenas;
    private readonly IExtractor<ComentarioRaw> _extractorComentarios;
    private readonly IStagingWriter<EncuestaRaw> _stagingEncuestas;
    private readonly IStagingWriter<ResenaRaw> _stagingResenas;
    private readonly IStagingWriter<ComentarioRaw> _stagingComentarios;
    private readonly IHostApplicationLifetime _ciclo;
    private readonly ILogger<ExtractionWorker> _logger;

    public ExtractionWorker(
        IExtractor<EncuestaRaw> extractorEncuestas,
        IExtractor<ResenaRaw> extractorResenas,
        IExtractor<ComentarioRaw> extractorComentarios,
        IStagingWriter<EncuestaRaw> stagingEncuestas,
        IStagingWriter<ResenaRaw> stagingResenas,
        IStagingWriter<ComentarioRaw> stagingComentarios,
        IHostApplicationLifetime ciclo,
        ILogger<ExtractionWorker> logger)
    {
        _extractorEncuestas = extractorEncuestas;
        _extractorResenas = extractorResenas;
        _extractorComentarios = extractorComentarios;
        _stagingEncuestas = stagingEncuestas;
        _stagingResenas = stagingResenas;
        _stagingComentarios = stagingComentarios;
        _ciclo = ciclo;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var reloj = Stopwatch.StartNew();
        _logger.LogInformation("=== INICIO DEL PROCESO DE EXTRACCION ===");

        var resultados = await Task.WhenAll(
            ProcesarAsync(_extractorEncuestas, _stagingEncuestas, stoppingToken),
            ProcesarAsync(_extractorResenas, _stagingResenas, stoppingToken),
            ProcesarAsync(_extractorComentarios, _stagingComentarios, stoppingToken));

        _logger.LogInformation("=== RESUMEN ===");
        foreach (var r in resultados)
            _logger.LogInformation("{Fuente}: {Estado} | extraídos {Extraidos} | guardados {Guardados}",
                r.Fuente, r.Exito ? "OK" : "ERROR", r.Extraidos, r.Guardados);

        _logger.LogInformation("=== FIN: {Ok} de {Total} fuentes correctas en {Seg:F1} s ===",
            resultados.Count(r => r.Exito), resultados.Length, reloj.Elapsed.TotalSeconds);

        _ciclo.StopApplication();
    }

    private async Task<ResultadoFuente> ProcesarAsync<T>(
        IExtractor<T> extractor, IStagingWriter<T> escritor, CancellationToken ct)
    {
        try
        {
            var registros = await extractor.ExtraerAsync(ct);
            int guardados = await escritor.GuardarAsync(registros, ct);
            return new ResultadoFuente(extractor.Nombre, true, registros.Count, guardados);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Falló la extracción de {Fuente}", extractor.Nombre);
            return new ResultadoFuente(extractor.Nombre, false, 0, 0);
        }
    }

    private record ResultadoFuente(string Fuente, bool Exito, int Extraidos, int Guardados);
}