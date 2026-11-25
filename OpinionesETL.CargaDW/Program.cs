using OpinionesETL.CargaDW;
using OpinionesETL.Core.Interfaces;
using OpinionesETL.Infrastructure.Configuracion;
using OpinionesETL.Infrastructure.Dimensiones;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<DataWarehouseOptions>(
    builder.Configuration.GetSection(DataWarehouseOptions.Seccion));

builder.Services.AddTransient<IDimensionLoader, DimClasificacionLoader>();
builder.Services.AddTransient<IDimensionLoader, DimCanalLoader>();
builder.Services.AddTransient<IDimensionLoader, DimFechaLoader>();
builder.Services.AddTransient<IDimensionLoader, DimProductoLoader>();
builder.Services.AddTransient<IDimensionLoader, DimClienteLoader>();

builder.Services.AddHostedService<DimensionWorker>();

var host = builder.Build();
host.Run();