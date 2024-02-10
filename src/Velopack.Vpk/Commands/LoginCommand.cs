namespace Velopack.Vpk.Commands;
public class LoginCommand : BaseCommand
{
    public string VelopackBaseUrl { get; private set; }

    public LoginCommand()
        : base("login", "Login to Vellopack's hosted service.")
    {
        //Just hiding this for now as it is not ready for mass consumption.
        Hidden = true;

        AddOption<string>(v => VelopackBaseUrl = v, "--baseUrl")
            .SetDescription("The base Uri for the Velopack API service.")
            .SetArgumentHelpName("URI")
            .SetDefault(VelopackServiceOptions.DefaultBaseUrl);
    }
}
