using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Web.Http;

var a = Singleton.CreateSingleton();

Animal A = new Dog();

A.DoSomething();
A.Sound();

A.Roar();

Dog d = new Dog();

d.DoSomething();
d.Sound();
d.Roar();

public abstract class Animal
{
    public string Name { get; }

    public string Description { get; }

    public abstract void Sound();

    public virtual void DoSomething()
    {
        Console.WriteLine("I'm not a human");
    }

    public virtual void Roar()
    {
        Console.WriteLine("LEROY Jankins");
    }
}

public class Dog : Animal
{
    private string youArentGonnaNeedIt;
    public new void DoSomething()
    {
        Console.WriteLine("I'm Dog");
    }

    public override void Sound()
    {
        Console.WriteLine("Bark");
    }
}

public class BLL(IDb context)
{
    public void AddPerson(Animal person)
    {
        context.QueryDb();
    }
}


public interface IDb
{
    void QueryDb();
}
public class DB : IDb
{
    public void QueryDb()
    {
        return;
    }
}


public class Singleton
{
    private static Singleton instance;

    private Singleton()
    {

    }

    public static Singleton CreateSingleton()
    {
        if(instance == null)
        {
            instance = new Singleton();
        }
        
        return instance;
    }
}

abstract class Decorator : Component
{
    protected Component _component;

    public Decorator(Component component)
    {
        this._component = component;
    }

    public void SetComponent(Component component)
    {
        this._component = component;
    }

    // Декоратор делегирует всю работу обёрнутому компоненту.
    public override string Operation()
    {
        if (this._component != null)
        {
            return this._component.Operation();
        }
        else
        {
            return string.Empty;
        }
    }
}

class ConcreteDecoratorA : Decorator
{
    public ConcreteDecoratorA(Component comp) : base(comp)
    {
    }

    // Декораторы могут вызывать родительскую реализацию операции, вместо
    // того, чтобы вызвать обёрнутый объект напрямую. Такой подход упрощает
    // расширение классов декораторов.
    public override string Operation()
    {
        return $"ConcreteDecoratorA({base.Operation()})";
    }
}

[Authorize]
[ApiController]
[Route("[controller]")]
public class GamesController(IDb context) : ControllerBase
{
    //роут с id из роута
    [AllowAnonymous]
    [HttpPost("/collection/{id}")]
    public IActionResult Create(int id)
    {
        return null;
    }
}

HashSet<int> b = new HashSet<int>();

Dictionary<int, string> s = new Dictionary<int, string>();

//IQueryable/IEnumerable - LINQ

