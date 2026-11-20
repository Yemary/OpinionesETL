using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesETL.Core.Interfaces;
using OpinionesETL.Core.Modelos;
using OpinionesETL.Infrastructure.Configuracion;

namespace OpinionesETL.Infrastructure.Extractores;

public class ApiExtractor : IExtractor<ComentarioRaw>
{
    private static readonly JsonSerializerOptions JsonOpciones = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly ApiOptions _opciones;
    private readonly ILogger<ApiExtractor> _logger;

    public ApiExtractor(HttpClient http, IOptions<FuentesOptions> opciones, ILogger<ApiExtractor> logger)
    {
        _http = http;
        _opciones = opciones.Value.Api;
        _logger = logger;
    }

    public string Nombre => "API REST (comentarios sociales)";

    public async Task<IReadOnlyList<ComentarioRaw>> ExtraerAsync(CancellationToken cancellationToken)
    {
        var reloj = Stopwatch.StartNew();
        string url = $"{_opciones.UrlBase.TrimEnd('/')}{_opciones.Ruta}";
        _logger.LogInformation("Iniciando extracción desde la API REST: {Url}", url);

        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(TimeSpan.FromSeconds(_opciones.TimeoutSegundos));

        try
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Get, url);
            peticion.Headers.Add("X-Api-Key", _opciones.ApiKey);

            using var respuesta = await _http.SendAsync(peticion, limite.Token);

            if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("La API rechazó la clave (401). Revise ApiKey en appsettings.json");
                throw new UnauthorizedAccessException("La API rechazó la clave de acceso.");
            }

            respuesta.EnsureSuccessStatusCode();

            var registros = await respuesta.Content.ReadFromJsonAsync<List<ComentarioRaw>>(JsonOpciones, limite.Token)
                            ?? new List<ComentarioRaw>();

            _logger.LogInformation("Extracción de API completada: {Cantidad} registros en {Ms} ms",
                registros.Count, reloj.ElapsedMilliseconds);
            return registros;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "No se pudo conectar con la API. ¿Está encendida?");
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("La API no respondió en {Seg} segundos", _opciones.TimeoutSegundos);
            throw new TimeoutException("La API no respondió a tiempo.");
        }
    }
}