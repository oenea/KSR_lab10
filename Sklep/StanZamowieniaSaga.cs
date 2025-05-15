using Kontrakty;
using MassTransit;
using System;
using System.Threading.Tasks; 

public class StanZamowieniaSaga : MassTransitStateMachine<StanZamowienia>
{
    public State? Oczekujacy { get; private set; }
    public State? Potwierdzony { get; private set; }
    public State? Odrzucony { get; private set; }

    public Event<StartZamowienia>? StartZamowieniaEvent { get; private set; }
    public Event<Potwierdzenie>? PotwierdzenieEvent { get; private set; }
    public Event<BrakPotwierdzenia>? BrakPotwierdzeniaEvent { get; private set; }
    public Event<OdpowiedzWolne>? OdpowiedzWolneEvent { get; private set; }
    public Event<OdpowiedzWolneNegatywna>? OdpowiedzWolneNegatywnaEvent { get; private set; }
    public Schedule<StanZamowienia, ZamowienieTimeout>? ZamowienieTimeoutSchedule
    {
        get;
        private set;
    }

    public StanZamowieniaSaga()
    {
        InstanceState(x => x.CurrentState);

        Event(() => StartZamowieniaEvent, x => x.CorrelateById(c => c.Message.IdKorelacji));
        Event(() => PotwierdzenieEvent, x => x.CorrelateById(c => c.Message.IdKorelacji));
        Event(() => BrakPotwierdzeniaEvent, x => x.CorrelateById(c => c.Message.IdKorelacji));
        Event(() => OdpowiedzWolneEvent, x => x.CorrelateById(c => c.Message.IdKorelacji));
        Event(() => OdpowiedzWolneNegatywnaEvent, x => x.CorrelateById(c => c.Message.IdKorelacji));
        Schedule(
            () => ZamowienieTimeoutSchedule,
            x => x.TimeoutTokenId,
            s =>
            {
                s.Delay = TimeSpan.FromSeconds(10);
                s.Received = x => x.CorrelateById(c => c.Message.IdKorelacji);
            }
        );

        Initially(
            When(StartZamowieniaEvent)
                .Then(context =>
                {
                    Console.WriteLine(
                        $"{ConsoleColors.Brown}[Sklep]{ConsoleColors.Reset} Zamówienie rozpoczęte przez klienta {ConsoleColors.Cyan}{context.Message.Klient}{ConsoleColors.Reset}, ilość: {ConsoleColors.Yellow}{context.Message.Ilosc}{ConsoleColors.Reset}"
                    );
                    context.Saga.Ilosc = context.Message.Ilosc;
                    context.Saga.Klient = context.Message.Klient;
                })
                .TransitionTo(Oczekujacy)
                .Schedule(
                    ZamowienieTimeoutSchedule,
                    context => new ZamowienieTimeout(context.Saga.CorrelationId)
                )
                .ThenAsync(async context =>
                {
                    Console.WriteLine(
                        $"{ConsoleColors.Brown}[Sklep]{ConsoleColors.Reset} Wysyłam zapytanie do magazynu o dostępność {ConsoleColors.Yellow}{context.Saga.Ilosc}{ConsoleColors.Reset} sztuk."
                    );
                    var endpoint = await context.GetSendEndpoint(new Uri("queue:magazyn"));
                    await endpoint.Send(
                        new PytanieOWolne(context.Saga.Ilosc, context.Saga.CorrelationId)
                    );

                    Console.WriteLine(
                        $"{ConsoleColors.Brown}[Sklep]{ConsoleColors.Reset} Wysyłam zapytanie do klienta {ConsoleColors.Cyan}{context.Saga.Klient}{ConsoleColors.Reset} o potwierdzenie zamówienia."
                    );
                    endpoint = await context.GetSendEndpoint(
                        new Uri($"queue:klient-{context.Saga.Klient}")
                    );
                    await endpoint.Send(
                        new PytanieOPotwierdzenie(context.Saga.Ilosc, context.Saga.CorrelationId)
                    );
                })
        );

        During(
            Oczekujacy,
            When(PotwierdzenieEvent)
                .Then(context =>
                {
                    Console.WriteLine(
                        $"{ConsoleColors.Brown}[Sklep]{ConsoleColors.Green} Klient {ConsoleColors.Cyan}{context.Saga.Klient}{ConsoleColors.Reset}{ConsoleColors.Green} potwierdził zamówienie na {ConsoleColors.Yellow}{context.Saga.Ilosc}{ConsoleColors.Reset}{ConsoleColors.Green} sztuk.{ConsoleColors.Reset}"
                    );
                    context.Saga.CzyKlientPotwierdzil = true;
                })
                .ThenAsync(SprobujZakonczyc),
            When(BrakPotwierdzeniaEvent)
                .TransitionTo(Odrzucony)
                .ThenAsync(async context =>
                {
                    Console.WriteLine(
                        $"{ConsoleColors.Brown}[Sklep]{ConsoleColors.Red} Klient {ConsoleColors.Cyan}{context.Saga.Klient}{ConsoleColors.Reset}{ConsoleColors.Red} nie potwierdził zamówienia na {ConsoleColors.Yellow}{context.Saga.Ilosc}{ConsoleColors.Reset}{ConsoleColors.Red} sztuk. Wysyłam do magazynu informację o odrzuceniu zamówienia.{ConsoleColors.Reset}"
                    );
                    var endpoint = await context.GetSendEndpoint(new Uri($"queue:magazyn"));
                    await endpoint.Send(
                        new OdrzucenieZamowienia(context.Saga.Ilosc, context.Saga.CorrelationId)
                    );
                }),
            When(OdpowiedzWolneEvent)
                .Then(context =>
                {
                    Console.WriteLine(
                        $"{ConsoleColors.Brown}[Sklep]{ConsoleColors.Green} Magazyn potwierdził dostępność {ConsoleColors.Yellow}{context.Saga.Ilosc}{ConsoleColors.Reset}{ConsoleColors.Green} sztuk.{ConsoleColors.Reset}"
                    );
                    context.Saga.CzyMagazynPotwierdzil = true;
                })
                .ThenAsync(SprobujZakonczyc),
            When(OdpowiedzWolneNegatywnaEvent)
                .TransitionTo(Odrzucony)
                .ThenAsync(async context =>
                {
                    Console.WriteLine(
                        $"{ConsoleColors.Brown}[Sklep]{ConsoleColors.Red} Magazyn nie potwierdził dostępności {ConsoleColors.Yellow}{context.Saga.Ilosc}{ConsoleColors.Reset}{ConsoleColors.Red} sztuk. Wysyłam do klienta informację o odrzuceniu zamówienia.{ConsoleColors.Reset}"
                    );
                    var endpoint = await context.GetSendEndpoint(
                        new Uri($"queue:klient-{context.Saga.Klient}")
                    );
                    await endpoint.Send(
                        new OdrzucenieZamowienia(context.Saga.Ilosc, context.Saga.CorrelationId)
                    );
                }),
            When(ZamowienieTimeoutSchedule!.Received)
                .TransitionTo(Odrzucony)
                .ThenAsync(async context =>
                {
                    Console.WriteLine(
                        $"{ConsoleColors.Brown}[Sklep]{ConsoleColors.Red} Zamówienie wygasło. Wysyłam do magazynu i klienta informację o odrzuceniu zamówienia.{ConsoleColors.Reset}"
                    );
                    var kontrakt = new OdrzucenieZamowienia(
                        context.Saga.Ilosc,
                        context.Saga.CorrelationId
                    );
                    var endpoint = await context.GetSendEndpoint(new Uri($"queue:magazyn"));
                    await endpoint.Send(kontrakt);
                    var endpoint2 = await context.GetSendEndpoint(
                        new Uri($"queue:klient-{context.Saga.Klient}")
                    );
                    await endpoint2.Send(kontrakt);
                })
        );
    }

    private async Task SprobujZakonczyc(BehaviorContext<StanZamowienia> context)
    {
        if (context.Saga.CzyKlientPotwierdzil && context.Saga.CzyMagazynPotwierdzil)
        {
            Console.WriteLine(
                $"{ConsoleColors.Brown}[Sklep]{ConsoleColors.Green} Zamówienie na {ConsoleColors.Yellow}{context.Saga.Ilosc}{ConsoleColors.Reset}{ConsoleColors.Green} sztuk zostało potwierdzone przez klienta {ConsoleColors.Cyan}{context.Saga.Klient}{ConsoleColors.Reset}{ConsoleColors.Green} i magazynu. Przechodzę do stanu potwierdzonego.{ConsoleColors.Reset}"
            );
            context.Saga.CurrentState = Potwierdzony?.Name ?? nameof(Potwierdzony);

            var kontrakt = new AkceptacjaZamowienia(context.Saga.Ilosc, context.Saga.CorrelationId);
            var endpoint = await context.GetSendEndpoint(
                new Uri($"queue:klient-{context.Saga.Klient}")
            );
            await endpoint.Send(kontrakt);

            endpoint = await context.GetSendEndpoint(new Uri($"queue:magazyn"));
            await endpoint.Send(kontrakt);
        }
    }
}
