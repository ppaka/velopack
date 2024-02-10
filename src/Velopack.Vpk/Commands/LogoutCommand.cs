namespace Velopack.Vpk.Commands;
public class LogoutCommand : VelopackServiceCommand
{
    public LogoutCommand()
        : base("logout", "Remove any stored credential to the Vellopack service.")
    {
        //Just hiding this for now as it is not ready for mass consumption.
        Hidden = true;
    }
}
