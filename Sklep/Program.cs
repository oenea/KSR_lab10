using Kontrakty;
using MassTransit;

var repo = new InMemorySagaRepository<StanZamowienia>();
var machine = new StanZamowieniaSaga();

var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
{
    cfg.Host(new Uri("rabbitmq://localhost/"),
        h => {
            h.Username("guest");
            h.Password("guest");
        });

    cfg.ReceiveEndpoint(
        "sklep",
        e =>
        {
            e.StateMachineSaga(machine, repo);
        }
    );

    cfg.UseInMemoryScheduler();
});

await bus.StartAsync();

Console.WriteLine("[Sklep] Rozpoczęto nasłuchiwanie na wiadomości. Naciśnij Enter, aby zakończyć.");
Console.ReadLine();

await bus.StopAsync();
