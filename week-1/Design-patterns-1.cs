using System;

class Logger
{
    // Static object
    private static Logger instance;

    // Private constructor
    private Logger()
    {
    }

    // Global access method
    public static Logger GetInstance()
    {
        if (instance == null)
        {
            instance = new Logger();
        }

        return instance;
    }

    public void PrintMessage(string message)
    {
        Console.WriteLine(message);
    }
}

class Program
{
    static void Main(string[] args)
    {
        Logger obj1 = Logger.GetInstance();
        Logger obj2 = Logger.GetInstance();

        obj1.PrintMessage("Singleton Pattern Example");

        Console.WriteLine(Object.ReferenceEquals(obj1, obj2));
    }
}