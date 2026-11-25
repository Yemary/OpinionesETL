using OpinionesETL.Core.Modelos;

namespace OpinionesETL.Core.Interfaces;

public interface IDimensionLoader
{
    string Nombre { get; }
    Task<ResultadoDimension> CargarAsync(CancellationToken cancellationToken);
}