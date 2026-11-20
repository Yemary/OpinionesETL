using OpinionesETL.Core.Modelos;

namespace OpinionesETL.Core.Interfaces;

public interface IExtractor<T>
{
    string Nombre { get; }
    Task<IReadOnlyList<T>> ExtraerAsync(CancellationToken cancellationToken);
}