using Kontrakty;
using MassTransit;
using System;
using System.Threading.Tasks;

int wolne = 0;
int zarezerwowane = 0;

var bus = Bus.Factory.CreateUsingRabbitMq(cfg =>
{
    cfg.Host(new Uri("rabbitmq://localhost/"),
        h => {
            h.Username("guest");
            h.Password("guest");
        });

    cfg.ReceiveEndpoint(
        "magazyn",
        e =>
        {
            e.Handler<PytanieOWolne>(async context =>
            {
                if (wolne >= context.Message.Ilosc)
                {
                    wolne -= context.Message.Ilosc;
                    zarezerwowane += context.Message.Ilosc;
                    Console.WriteLine(
                        $"{ConsoleColors.Green}[Magazyn]{ConsoleColors.Reset} Klient zarezerwował {ConsoleColors.Cyan}{context.Message.Ilosc}{ConsoleColors.Reset} sztuk. {ConsoleColors.Yellow}Wolne:{ConsoleColors.Reset} {wolne}, {ConsoleColors.Magenta}Zarezerwowane:{ConsoleColors.Reset} {zarezerwowane}"
                    );
                    await context.RespondAsync(new OdpowiedzWolne(context.Message.IdKorelacji));
                }
                else
                {
                    Console.WriteLine(
                        $"{ConsoleColors.Red}[Magazyn]{ConsoleColors.Reset} Liczba wolnych jednostek ({ConsoleColors.Yellow}{wolne}{ConsoleColors.Reset}) jest mniejsza niż zamówiona ({ConsoleColors.Cyan}{context.Message.Ilosc}{ConsoleColors.Reset}). Nie można zrealizować zamówienia. {ConsoleColors.Yellow}Wolne:{ConsoleColors.Reset} {wolne}, {ConsoleColors.Magenta}Zarezerwowane:{ConsoleColors.Reset} {zarezerwowane}"
                    );
                    await context.RespondAsync(
                        new OdpowiedzWolneNegatywna(context.Message.IdKorelacji)
                    );
                }
            });

            e.Handler<AkceptacjaZamowienia>(context =>
            {
                zarezerwowane -= context.Message.Ilosc;
                Console.WriteLine(
                    $"{ConsoleColors.Green}[Magazyn]{ConsoleColors.Reset} Zamówienie na {ConsoleColors.Cyan}{context.Message.Ilosc}{ConsoleColors.Reset} sztuk zostało zaakceptowane. {ConsoleColors.Yellow}Wolne:{ConsoleColors.Reset} {wolne}, {ConsoleColors.Magenta}Zarezerwowane:{ConsoleColors.Reset} {zarezerwowane}"
                );
                return Task.CompletedTask;
            });

            e.Handler<OdrzucenieZamowienia>(context =>
            {
                wolne += context.Message.Ilosc;
                zarezerwowane -= context.Message.Ilosc;
                Console.WriteLine(
                    $"{ConsoleColors.Red}[Magazyn]{ConsoleColors.Reset} Zamówienie na {ConsoleColors.Cyan}{context.Message.Ilosc}{ConsoleColors.Reset} sztuk zostało odrzucone. {ConsoleColors.Yellow}Wolne:{ConsoleColors.Reset} {wolne}, {ConsoleColors.Magenta}Zarezerwowane:{ConsoleColors.Reset} {zarezerwowane}"
                );
                return Task.CompletedTask;
            });
        }
    );
});

await bus.StartAsync();

Console.WriteLine($"{ConsoleColors.Blue}[Magazyn]{ConsoleColors.Reset} Wpisz ilość sztuk do dodania (lub wciśnij Enter, aby zakończyć):");
while (true)
{
    var input = Console.ReadLine();
    if (string.IsNullOrEmpty(input))
        break;

    if (int.TryParse(input, out var ilosc))
    {
        wolne += ilosc;
        Console.WriteLine(
            $"{ConsoleColors.Blue}[Magazyn]{ConsoleColors.Reset} Dodano {ConsoleColors.Cyan}{ilosc}{ConsoleColors.Reset} sztuk do stanu magazynowego. {ConsoleColors.Yellow}Wolne:{ConsoleColors.Reset} {wolne}, {ConsoleColors.Magenta}Zarezerwowane:{ConsoleColors.Reset} {zarezerwowane}"
        );
    }
    else
    {
        Console.WriteLine($"{ConsoleColors.Red}[Magazyn]{ConsoleColors.Reset} Wprowadzono nieprawidłową wartość. Proszę podać liczbę.");
    }
}

await bus.StopAsync();