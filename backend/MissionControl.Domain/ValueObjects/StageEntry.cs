namespace MissionControl.Domain.ValueObjects;

public sealed record StageEntry
{
    public string PartId { get; init; }
    public int Quantity { get; init; }

    public StageEntry(string partId, int quantity)
    {
        ArgumentNullException.ThrowIfNull(partId, nameof(partId));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least 1.");
        PartId = partId;
        Quantity = quantity;
    }
}
