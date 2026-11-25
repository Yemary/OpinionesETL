using System.Data;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpinionesETL.Infrastructure.Configuracion;

namespace OpinionesETL.Infrastructure.Dimensiones;

public class DimFechaLoader : DimensionLoaderBase
{
    private static readonly string[] Meses =
    {
        "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
        "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre"
    };

    private static readonly string[] Dias =
    {
        "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"
    };

    public DimFechaLoader(IOptions<DataWarehouseOptions> opciones, ILogger<DimFechaLoader> logger)
        : base(opciones, logger) { }

    public override string Nombre => "DimFecha";

    protected override string TablaTemporal =>
        @"CREATE TABLE #src (
            IdFecha       INT          NOT NULL,
            Fecha         DATE         NOT NULL,
            Anio          SMALLINT     NOT NULL,
            Trimestre     TINYINT      NOT NULL,
            Mes           TINYINT      NOT NULL,
            NombreMes     NVARCHAR(20) NOT NULL,
            AnioMes       CHAR(7)      NOT NULL,
            AnioTrimestre CHAR(7)      NOT NULL,
            NombreDia     NVARCHAR(20) NOT NULL);";

    protected override string SqlMerge =>
        @"MERGE dbo.DimFecha AS t
          USING #src AS s ON t.IdFecha = s.IdFecha
          WHEN NOT MATCHED BY TARGET
              THEN INSERT (IdFecha, Fecha, Anio, Trimestre, Mes, NombreMes, AnioMes, AnioTrimestre, NombreDia)
                   VALUES (s.IdFecha, s.Fecha, s.Anio, s.Trimestre, s.Mes, s.NombreMes, s.AnioMes, s.AnioTrimestre, s.NombreDia)
          OUTPUT $action;";

    protected override Task<DataTable> LeerOrigenAsync(CancellationToken ct)
    {
        var inicio = DateTime.ParseExact(Opciones.FechaInicio, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var fin = DateTime.ParseExact(Opciones.FechaFin, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (fin < inicio)
            throw new InvalidOperationException("FechaFin no puede ser anterior a FechaInicio.");

        var tabla = new DataTable();
        tabla.Columns.Add("IdFecha", typeof(int));
        tabla.Columns.Add("Fecha", typeof(DateTime));
        tabla.Columns.Add("Anio", typeof(short));
        tabla.Columns.Add("Trimestre", typeof(byte));
        tabla.Columns.Add("Mes", typeof(byte));
        tabla.Columns.Add("NombreMes", typeof(string));
        tabla.Columns.Add("AnioMes", typeof(string));
        tabla.Columns.Add("AnioTrimestre", typeof(string));
        tabla.Columns.Add("NombreDia", typeof(string));

        for (var d = inicio; d <= fin; d = d.AddDays(1))
        {
            int trimestre = (d.Month - 1) / 3 + 1;
            tabla.Rows.Add(
                d.Year * 10000 + d.Month * 100 + d.Day,
                d,
                (short)d.Year,
                (byte)trimestre,
                (byte)d.Month,
                Meses[d.Month - 1],
                $"{d.Year}-{d.Month:00}",
                $"{d.Year}-T{trimestre}",
                Dias[(int)d.DayOfWeek]);
        }
        return Task.FromResult(tabla);
    }
}