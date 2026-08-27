namespace Starlight.Protocol;

/// <summary>
/// A substitute for <see cref="OpcodeAttribute"/>.
/// <br/>
/// Marks a method as a <b>lifecycle event handler</b>.
/// See <see cref="LifecycleEvent"/> for all possible events.
/// <br/>
/// Handlers take no message, only the session player. Anything they return is sent to the client.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LifecycleAttribute(LifecycleEvent @event) : Attribute
{
    public LifecycleEvent Event => @event;
}

public enum LifecycleEvent
{
    /// Sent once <c>PlayerLoginReq</c> has loaded the player's data, before <c>PlayerLoginRsp</c> goes out.
    PlayerLogin,
    /// Sent when the KCP session is dropped. The tunnel is gone by now, so sends are discarded.
    PlayerDisconnect
}
