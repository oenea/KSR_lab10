using MassTransit;

public class StanZamowienia : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;

    public int Ilosc { get; set; }
    public string Klient { get; set; } = string.Empty;
    public bool CzyKlientPotwierdzil { get; set; }
    public bool CzyMagazynPotwierdzil { get; set; }
    public Guid? TimeoutTokenId { get; set; }
}
