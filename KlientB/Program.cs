using Kontrakty;
using MassTransit;
using System;
using System.Threading.Tasks;

var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
{
    cfg.Host(new Uri("rabbitmq://localhost/"),
        h => {
            h.Username("guest");
            h.Password("guest");
        });

    cfg.ReceiveEndpoint(
        "klient-b",
        e =>
        {
            e.Handler<PytanieOPotwierdzenie>(async context =>
            {
                Console.WriteLine(
                    $"{ConsoleColors.Pink}[Klient B]{ConsoleColors.Reset} Otrzymano pytanie o potwierdzenie na {ConsoleColors.Yellow}{context.Message.Ilosc}{ConsoleColors.Reset} sztuk. Potwierdzić? ({ConsoleColors.Green}t{ConsoleColors.Reset}/{ConsoleColors.Red}n{ConsoleColors.Reset})"
                );
                var klawisz = Console.ReadKey(true);
                Console.WriteLine();

                if (klawisz.Key == ConsoleKey.T)
                {
                    Console.WriteLine(
                        $"{ConsoleColors.Pink}[Klient B]{ConsoleColors.Green} Potwierdzam zamówienie na {ConsoleColors.Yellow}{context.Message.Ilosc}{ConsoleColors.Reset}{ConsoleColors.Green} sztuk.{ConsoleColors.Reset}"
                    );
                    await context.RespondAsync(new Potwierdzenie(context.Message.IdKorelacji));
                }
                else
                {
                    Console.WriteLine(
                        $"{ConsoleColors.Pink}[Klient B]{ConsoleColors.Red} Nie potwierdzam zamówienia na {ConsoleColors.Yellow}{context.Message.Ilosc}{ConsoleColors.Reset}{ConsoleColors.Red} sztuk.{ConsoleColors.Reset}"
                    );
                    await context.RespondAsync(new BrakPotwierdzenia(context.Message.IdKorelacji));
                }
            });

            e.Handler<AkceptacjaZamowienia>(context =>
            {
                Console.WriteLine(
                    $"{ConsoleColors.Pink}[Klient B]{ConsoleColors.Green} Zamówienie na {ConsoleColors.Yellow}{context.Message.Ilosc}{ConsoleColors.Reset}{ConsoleColors.Green} sztuk zostało zaakceptowane.{ConsoleColors.Reset}"
                );
                return Task.CompletedTask;
            });

            e.Handler<OdrzucenieZamowienia>(context =>
            {
                Console.WriteLine(
                    $"{ConsoleColors.Pink}[Klient B]{ConsoleColors.Red} Zamówienie na {ConsoleColors.Yellow}{context.Message.Ilosc}{ConsoleColors.Reset}{ConsoleColors.Red} sztuk zostało odrzucone.{ConsoleColors.Reset}"
                );
                return Task.CompletedTask;
            });
        }
    );
});

await bus.StartAsync();

Console.WriteLine($"{ConsoleColors.Pink}[Klient B]{ConsoleColors.Reset} Naciśnij {ConsoleColors.Yellow}Z{ConsoleColors.Reset}, aby wysłać zamówienie. Naciśnij {ConsoleColors.Yellow}Enter{ConsoleColors.Reset}, aby zakończyć.");

while (true)
{
    var key = Console.ReadKey(true);
    if (key.Key == ConsoleKey.Enter)
        break;
    if (key.Key != ConsoleKey.Z)
        continue;

    Console.Write($"{ConsoleColors.Pink}[Klient B]{ConsoleColors.Reset} Wpisz ilość sztuk do zamówienia: ");
    var input = Console.ReadLine();
    if (int.TryParse(input, out var ilosc) && ilosc > 0)
    {
        var idKorelacji = Guid.NewGuid();
        Console.WriteLine($"{ConsoleColors.Pink}[Klient B]{ConsoleColors.Reset} Wysyłanie zamówienia na {ConsoleColors.Yellow}{ilosc}{ConsoleColors.Reset} sztuk...");
        var endpoint = await bus.GetSendEndpoint(new Uri("queue:sklep"));
        await endpoint.Send(new StartZamowienia(ilosc, "b", idKorelacji));
    }
    else
    {
        Console.WriteLine($"{ConsoleColors.Pink}[Klient B]{ConsoleColors.Red} Niepoprawna ilość sztuk. Proszę podać liczbę dodatnią.{ConsoleColors.Reset}");
    }

    await Task.Delay(1000);
}

await bus.StopAsync();
