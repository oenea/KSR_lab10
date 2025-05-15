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
    public const string Black = "\x1b[30m";
    public const string BrightBlack = "\x1b[90m";
    public const string BrightRed = "\x1b[91m";
    public const string BrightGreen = "\x1b[92m";
    public const string BrightYellow = "\x1b[93m";
    public const string BrightBlue = "\x1b[94m";
    public const string BrightMagenta = "\x1b[95m";
    public const string BrightCyan = "\x1b[96m";
    public const string BrightWhite = "\x1b[97m";
    public const string DarkRed = "\x1b[31;2m";
    public const string DarkGreen = "\x1b[32;2m";
    public const string DarkYellow = "\x1b[33;2m";
    public const string DarkBlue = "\x1b[34;2m";
    public const string DarkMagenta = "\x1b[35;2m";
    public const string DarkCyan = "\x1b[36;2m";
    public const string LightGray = "\x1b[37m";
    public const string Gray = "\x1b[90m"; 
    public const string Orange = "\x1b[38;5;208m";
    public const string Pink = "\x1b[38;5;213m";
    public const string Violet = "\x1b[38;5;141m";
    public const string Brown = "\x1b[38;5;94m";
}