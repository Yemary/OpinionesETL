namespace OpinionesETL.Core.Modelos;

public record ResultadoDimension(string Dimension, int Leidos, int Insertados, int Actualizados, long DuracionMs);