namespace Kontrakty;
public record StartZamowienia(int Ilosc, string Klient, Guid IdKorelacji);
public record PytanieOPotwierdzenie(int Ilosc, Guid IdKorelacji);
public record Potwierdzenie(Guid IdKorelacji);
public record BrakPotwierdzenia(Guid IdKorelacji);
public record PytanieOWolne(int Ilosc, Guid IdKorelacji);
public record OdpowiedzWolne(Guid IdKorelacji);
public record OdpowiedzWolneNegatywna(Guid IdKorelacji);
public record AkceptacjaZamowienia(int Ilosc, Guid IdKorelacji);
public record OdrzucenieZamowienia(int Ilosc, Guid IdKorelacji);
public record ZamowienieTimeout(Guid IdKorelacji);

public static class ConsoleColors
{
    public const string Reset = "\x1b[0m";
    public const string Green = "\x1b[32m";
    public const string Red = "\x1b[31m";
    public const string Yellow = "\x1b[33m";
    public const string Blue = "\x1b[34m";
    public const string Magenta = "\x1b[35m";
    public const string Cyan = "\x1b[36m";
}