using FinanzasInteligentes.Infraestructura;
using FinanzasInteligentes.Worker.Analitica;
using FinanzasInteligentes.Worker.Outbox;
using FinanzasInteligentes.Worker.Recurrencias;
using FinanzasInteligentes.Worker.Suscripciones;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPersistencia(builder.Configuration);
builder.Services.AddHostedService<OutboxWorker>();
builder.Services.AddHostedService<RecurrenciasWorker>();
builder.Services.AddHostedService<AnaliticaWorker>();
builder.Services.AddHostedService<SuscripcionesWorker>();

var host = builder.Build();

host.Run();
