namespace OpinionesETL.Infrastructure.Configuracion;

public class DataWarehouseOptions
{
    public const string Seccion = "DataWarehouse";
    public string CadenaConexionOrigen { get; set; } = string.Empty;
    public string CadenaConexionDW { get; set; } = string.Empty;
    public string FechaInicio { get; set; } = "2020-01-01";
    public string FechaFin { get; set; } = "2030-12-31";
}