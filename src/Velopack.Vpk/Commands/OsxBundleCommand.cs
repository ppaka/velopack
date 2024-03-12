﻿namespace Velopack.Vpk.Commands;

public class OsxBundleCommand : PlatformCommand
{
    public string PackId { get; private set; }

    public string PackVersion { get; private set; }

    public string PackDirectory { get; private set; }

    public string PackAuthors { get; private set; }

    public string PackTitle { get; private set; }

    public string EntryExecutableName { get; private set; }

    public string Icon { get; private set; }

    public string BundleId { get; private set; }

    public string InfoPlistPath { get; private set; }

    public OsxBundleCommand()
        : this("bundle", "Create's an OSX .app bundle from a folder containing application files.")
    { }

    public OsxBundleCommand(string name, string description)
        : base(name, description)
    {
        AddOption<string>((v) => PackId = v, "--packId", "-u")
            .SetDescription("Unique Id for application bundle.")
            .SetArgumentHelpName("ID")
            .SetRequired()
            .RequiresValidNuGetId();

        // TODO add parser straight to SemanticVersion?
        AddOption<string>((v) => PackVersion = v, "--packVersion", "-v")
            .SetDescription("Current version for application bundle.")
            .SetArgumentHelpName("VERSION")
            .SetRequired()
            .RequiresSemverCompliant();

        AddOption<DirectoryInfo>((v) => PackDirectory = v.ToFullNameOrNull(), "--packDir", "-p")
            .SetDescription("Directory containing application files for release.")
            .SetArgumentHelpName("DIR")
            .SetRequired()
            .MustNotBeEmpty();

        AddOption<string>((v) => PackAuthors = v, "--packAuthors")
            .SetDescription("Company name or comma-delimited list of authors.")
            .SetArgumentHelpName("AUTHORS");

        AddOption<string>((v) => PackTitle = v, "--packTitle")
            .SetDescription("Display/friendly name for application.")
            .SetArgumentHelpName("NAME");

        AddOption<string>((v) => EntryExecutableName = v, "-e", "--mainExe")
            .SetDescription("The file name of the main/entry executable.")
            .SetArgumentHelpName("NAME");

        AddOption<FileInfo>((v) => Icon = v.ToFullNameOrNull(), "-i", "--icon")
            .SetDescription("Path to the .icns file for this bundle.")
            .SetArgumentHelpName("PATH")
            .MustExist()
            .RequiresExtension(".icns");

        var bundleId = AddOption<string>((v) => BundleId = v, "--bundleId")
            .SetDescription("Optional Apple bundle Id.")
            .SetArgumentHelpName("ID");

        var infoPlist = AddOption<FileInfo>((v) => InfoPlistPath = v.ToFullNameOrNull(), "--plist")
            .SetDescription("A custom Info.plist to use in the app bundle.")
            .SetArgumentHelpName("PATH")
            .MustExist();

        this.AreMutuallyExclusive(bundleId, infoPlist);
    }
}
