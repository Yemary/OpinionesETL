using Microsoft.VisualBasic.FileIO;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string rutaCsv = app.Configuration["RutaCsv"]
    ?? throw new InvalidOperationException("Falta RutaCsv en appsettings.json");
string apiKey = app.Configuration["ApiKey"] ?? string.Empty;

app.MapGet("/api/salud", () => Results.Ok(new { estado = "activo" }));

app.MapGet("/api/comentarios", (HttpContext contexto) =>
{
    if (!string.Equals(contexto.Request.Headers["X-Api-Key"].ToString(), apiKey))
        return Results.Unauthorized();

    var comentarios = new List<object>();
    using var parser = new TextFieldParser(rutaCsv);
    parser.TextFieldType = FieldType.Delimited;
    parser.SetDelimiters(",");
    parser.HasFieldsEnclosedInQuotes = true;

    if (!parser.EndOfData) parser.ReadFields(); 

    while (!parser.EndOfData)
    {
        var c = parser.ReadFields();
        if (c == null || c.Length < 6) continue;

        comentarios.Add(new
        {
            IdComment = c[0].Trim(),
            IdCliente = c[1].Trim(),
            IdProducto = c[2].Trim(),
            Fuente = c[3].Trim(),
            Fecha = c[4].Trim(),
            Comentario = c[5].Trim()
        });
    }

    return Results.Ok(comentarios);
});

app.Run("http://localhost:5100");