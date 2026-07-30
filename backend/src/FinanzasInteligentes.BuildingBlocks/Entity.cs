namespace FinanzasInteligentes.BuildingBlocks;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.CreateVersion7();
    public DateTimeOffset CreadoEn { get; protected set; } = DateTimeOffset.UtcNow;
}

public abstract class MutableEntity : Entity
{
    public DateTimeOffset ActualizadoEn { get; protected set; } = DateTimeOffset.UtcNow;
    public long Version { get; protected set; } = 1;

    protected void Touch()
    {
        ActualizadoEn = DateTimeOffset.UtcNow;
        Version = checked(Version + 1);
    }
}