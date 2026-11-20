using OpinionesETL.Core.Interfaces;
using OpinionesETL.Core.Modelos;
using OpinionesETL.Infrastructure.Configuracion;
using OpinionesETL.Infrastructure.Extractores;
using OpinionesETL.Infrastructure.Staging;
using OpinionesETL.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Configuración desde appsettings.json
builder.Services.Configure<FuentesOptions>(builder.Configuration.GetSection(FuentesOptions.Seccion));
builder.Services.Configure<StagingOptions>(builder.Configuration.GetSection(StagingOptions.Seccion));

// Extractores: cada uno cumple el contrato IExtractor<T>
builder.Services.AddTransient<IExtractor<EncuestaRaw>, CsvExtractor>();
builder.Services.AddTransient<IExtractor<ResenaRaw>, DatabaseExtractor>();
builder.Services.AddHttpClient<IExtractor<ComentarioRaw>, ApiExtractor>();

// Escritor de staging (uno por tipo de modelo)
builder.Services.AddTransient(typeof(IStagingWriter<>), typeof(SqlStagingWriter<>));

builder.Services.AddHostedService<ExtractionWorker>();

var host = builder.Build();
host.Run();