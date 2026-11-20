using System.Diagnostics;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesETL.Core.Interfaces;
using OpinionesETL.Core.Modelos;
using OpinionesETL.Infrastructure.Configuracion;

namespace OpinionesETL.Infrastructure.Extractores;

public class CsvExtractor : IExtractor<EncuestaRaw>
{
    private readonly CsvOptions _opciones;
    private readonly ILogger<CsvExtractor> _logger;

    public CsvExtractor(IOptions<FuentesOptions> opciones, ILogger<CsvExtractor> logger)
    {
        _opciones = opciones.Value.Csv;
        _logger = logger;
    }

    public string Nombre => "CSV (encuestas)";

    public async Task<IReadOnlyList<EncuestaRaw>> ExtraerAsync(CancellationToken cancellationToken)
    {
        var reloj = Stopwatch.StartNew();
        string ruta = Path.Combine(_opciones.Carpeta, _opciones.ArchivoEncuestas);
        _logger.LogInformation("Iniciando extracción desde CSV: {Ruta}", ruta);

        if (!File.Exists(ruta))
        {
            _logger.LogError("No se encontró el archivo CSV: {Ruta}", ruta);
            throw new FileNotFoundException("No se encontró el archivo de encuestas.", ruta);
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            PrepareHeaderForMatch = args => Normalizar(args.Header)
        };

        var registros = new List<EncuestaRaw>();
        using var lector = new StreamReader(ruta, Encoding.UTF8);
        using var csv = new CsvReader(lector, config);

        await foreach (var registro in csv.GetRecordsAsync<EncuestaRaw>(cancellationToken))
        {
            registros.Add(registro);
        }

        _logger.LogInformation("Extracción CSV completada: {Cantidad} registros en {Ms} ms",
            registros.Count, reloj.ElapsedMilliseconds);
        return registros;
    }

    private static string Normalizar(string texto)
    {
        var descompuesto = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in descompuesto)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Trim().ToLowerInvariant();
    }
}