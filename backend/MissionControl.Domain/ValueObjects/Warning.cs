using MissionControl.Domain.Enums;

namespace MissionControl.Domain.ValueObjects;

public sealed record Warning(WarningType Type, string Message, bool IsBlocking);
