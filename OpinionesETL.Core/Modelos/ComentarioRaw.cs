namespace OpinionesETL.Core.Modelos;

public class ComentarioRaw
{
    public string IdComment { get; set; } = string.Empty;
    public string IdCliente { get; set; } = string.Empty;
    public string IdProducto { get; set; } = string.Empty;
    public string Fuente { get; set; } = string.Empty;
    public string Fecha { get; set; } = string.Empty;
    public string Comentario { get; set; } = string.Empty;
}