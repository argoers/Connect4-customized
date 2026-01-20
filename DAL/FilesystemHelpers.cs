namespace DAL;

public class FilesystemHelpers
{
    private const string AppName = "Connect4";
    
    public static string GetConfigDirectory()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var finalDirectory = homeDirectory + Path.DirectorySeparatorChar + AppName + Path.DirectorySeparatorChar + "configs";
        
        Directory.CreateDirectory(finalDirectory);
        return finalDirectory;
    }
    
    public static string GetGameDirectory()
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var finalDirectory = homeDirectory + Path.DirectorySeparatorChar + AppName + Path.DirectorySeparatorChar + "savegames";
        
        Directory.CreateDirectory(finalDirectory);
        return finalDirectory;
    }
}