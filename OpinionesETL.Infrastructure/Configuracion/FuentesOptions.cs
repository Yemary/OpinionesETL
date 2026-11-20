namespace OpinionesETL.Infrastructure.Configuracion;

public class FuentesOptions
{
    public const string Seccion = "Fuentes";
    public CsvOptions Csv { get; set; } = new();
    public BaseDatosOptions BaseDatos { get; set; } = new();
    public ApiOptions Api { get; set; } = new();
}

public class CsvOptions
{
    public string Carpeta { get; set; } = string.Empty;
    public string ArchivoEncuestas { get; set; } = string.Empty;
}

public class BaseDatosOptions
{
    public string CadenaConexion { get; set; } = string.Empty;
    public string Consulta { get; set; } = string.Empty;
}

public class ApiOptions
{
    public string UrlBase { get; set; } = string.Empty;
    public string Ruta { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSegundos { get; set; } = 30;
}