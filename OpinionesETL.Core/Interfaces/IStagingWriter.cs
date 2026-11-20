namespace OpinionesETL.Core.Interfaces;

public interface IStagingWriter<T>
{
    Task<int> GuardarAsync(IReadOnlyList<T> registros, CancellationToken cancellationToken);
}