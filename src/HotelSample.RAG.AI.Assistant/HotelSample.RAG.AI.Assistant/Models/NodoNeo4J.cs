namespace HotelSample.RAG.AI.Assistant.Models;

public enum Environment { Host, Distrib }

internal class NodoNeo4J
{
    public string? Id { get; set; }
    public string? IdPadre { get; set; }
    public string? Name { get; set; }
    public Environment Environment { get; set; }
    public string? Proceso { get; set; }
    public string? Objetivo { get; set; }
    public List<string> Labels { get; set; } = new List<string>();
    public List<NodoNeo4J> Hijos { get; set; } = new List<NodoNeo4J>();


    public NodoNeo4J(string? id, string? idPadre, string? name, string? environment, string? proceso, string? objetivo, List<string>? labels)
    {
        Id = id;
        IdPadre = idPadre;
        Name = name;
        Proceso = proceso;
        Objetivo = objetivo;
        Labels = labels ?? [];

        if (environment == null || environment.ToUpper().Trim() == "MAINFRAME")
            Environment = Environment.Host;
        else
            Environment = Environment.Distrib;
    }

    public override string ToString()
    {
        string labels = Labels != null ? string.Join(",", Labels) : "";
        string salida = $"Id: {Id}\nName: {Name}\nEnvironment: {Environment}\nLabels:{labels}";
        return salida;
    }

    public string ToStringConsole(bool aplicarColor = false)
    {
        if (!aplicarColor)
            return @$"{Name} - ({string.Join(",", Labels ?? [])}/{Environment.ToString()})";

        string color = "Default";

        if (Environment == Environment.Host)
            color = "Yellow";
        else if (Environment == Environment.Distrib)
            color = "Cyan";

        return @$"[{color}]{Name} - ({string.Join(",", Labels ?? [])}/{Environment.ToString()}[/])";
    }
}

