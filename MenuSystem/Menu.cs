namespace MenuSystem;

public class Menu
{
    private string Title { get; set; } = default!;
    private Dictionary<string, MenuItem> MenuItems { get; set; } = new();
    
    private EMenuLevel Level { get; set; }
    
    private const string invalidInputMessage = "Invalid input. Please try again.";

    public void AddMenuItem(string key, string value, Func<string> methodToRun)
    {
        if (key == MenuDefaults.ExitKey || key == MenuDefaults.BackKey || key == MenuDefaults.MainMenuKey)
        {
            throw new ArgumentException($"Key {key} is among basic keys and can not be used otherwise");
        }
        
        if (MenuItems.ContainsKey(key))
        {
            throw new ArgumentException($"Menu item with key '{key}' already exists");
        }

        MenuItems[key] = new MenuItem() { Key = key, Value = value, MethodToRun = methodToRun };
    }

    public Menu(string title, EMenuLevel level)
    {
        Title = title;
        Level = level;
        
        switch (level)
        {
            case EMenuLevel.Root:
                MenuItems[MenuDefaults.ExitKey] = new MenuItem() {Key = MenuDefaults.ExitKey,  Value = MenuDefaults.ExitLabel};
                break;
            case EMenuLevel.First:
                MenuItems[MenuDefaults.MainMenuKey] = new MenuItem() {Key = MenuDefaults.MainMenuKey, Value = MenuDefaults.MainMenuLabel};
                MenuItems[MenuDefaults.ExitKey] = new MenuItem() { Key = MenuDefaults.ExitKey, Value = MenuDefaults.ExitLabel };
                break;
            case EMenuLevel.Deep:
                MenuItems[MenuDefaults.BackKey] = new MenuItem() { Key = MenuDefaults.BackKey, Value = MenuDefaults.BackLabel };
                MenuItems[MenuDefaults.MainMenuKey] = new MenuItem() {Key = MenuDefaults.MainMenuKey, Value = MenuDefaults.MainMenuLabel};
                MenuItems[MenuDefaults.ExitKey] = new MenuItem() { Key = MenuDefaults.ExitKey, Value = MenuDefaults.ExitLabel };
                break;
                
        }
    }

    public string Run()
    {
        Console.Clear();
        var menuRunning = true;
        var userChoice = string.Empty;

        do
        {
            DisplayMenu();
            Console.Write("Select an option: ");
            
            var input = Console.ReadLine(); 
            if (input == null)
            {
                Console.WriteLine(invalidInputMessage);
                continue;
            }

            userChoice = input.Trim().ToLower();
            if (userChoice == MenuDefaults.ExitKey || userChoice == MenuDefaults.MainMenuKey || userChoice == MenuDefaults.BackKey) 
            { 
                if (userChoice == MenuDefaults.ExitKey)
                {
                    menuRunning = false; 
                }
                else if (userChoice == MenuDefaults.MainMenuKey && Level != EMenuLevel.Root)
                {
                    menuRunning = false;
                    userChoice = MenuDefaults.MainMenuKey;
                }
                else if (userChoice == MenuDefaults.BackKey && Level == EMenuLevel.Deep)
                {
                    menuRunning = false;
                    userChoice = MenuDefaults.BackKey;
                }
                else
                {
                    Console.WriteLine(invalidInputMessage);
                }
            }
            else
            {
                if (MenuItems.ContainsKey(userChoice))
                {
                    var returnValueFromMethodToRun = MenuItems[userChoice].MethodToRun?.Invoke();
                    if (returnValueFromMethodToRun == MenuDefaults.ExitKey)
                    {
                        menuRunning = false;
                        userChoice = MenuDefaults.ExitKey;
                    }
                    else if (returnValueFromMethodToRun == MenuDefaults.MainMenuKey && Level != EMenuLevel.Root)
                    {
                        menuRunning = false;
                        userChoice = MenuDefaults.MainMenuKey;
                    }
                }
                else
                {
                    Console.WriteLine(invalidInputMessage);
                }
                Console.WriteLine();
            }
        } while (menuRunning);
        Console.Clear();

        return userChoice;
    }
    public void DisplayMenu()
    {
        Console.WriteLine(Title);
        Console.WriteLine("------------------------");
        foreach (var item in MenuItems.Values)
        {
            Console.WriteLine(item);
        }
    }
}