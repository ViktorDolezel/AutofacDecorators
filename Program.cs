using Autofac; //Version="6.4.0"
using NUnit.Framework;
using System.Reflection;

[TestFixture]
public class AutofacTest
{
    [Test]
    public void ResolveAllCommandHandlers_IsSuccessful()
    {
        var builder = new ContainerBuilder();

        // this works as well instead of the manual registrations below
        //builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
        //    .AsClosedTypesOf(typeof(IHandler<>))
        //    .AsImplementedInterfaces();

        builder.RegisterType(typeof(CommandHandler1))
            .As(typeof(ICommandHandler<Command>))
            .As(typeof(IHandler<Command>));
        builder.RegisterType(typeof(CommandHandler2))
            .As(typeof(ICommandHandler<Command>))
            .As(typeof(IHandler<Command>));


        var container = builder.Build();

        var commandHandlers = ((IEnumerable<IHandler<Command>>)container.Resolve(typeof(IEnumerable<IHandler<Command>>))).ToList();
        Assert.AreEqual(commandHandlers[0].GetType(), typeof(CommandHandler1));
        Assert.AreEqual(commandHandlers[1].GetType(), typeof(CommandHandler2));

    }

    [Test]
    public void ResolveAllDecoratedCommandHandlers_Manual_IsSucessful()
    {
        var builder = new ContainerBuilder();

        builder.RegisterType(typeof(CommandHandler1))
            .As(typeof(ICommandHandler<Command>))
            .As(typeof(IHandler<Command>));
        builder.RegisterType(typeof(CommandHandler2))
            .As(typeof(ICommandHandler<Command>))
            .As(typeof(IHandler<Command>));


        builder.RegisterGenericDecorator(
            typeof(CommandHandlerDecorator<>),
            typeof(IHandler<>),
            context => context.ImplementationType.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
        );

        var container = builder.Build();

        var commandHandlers = ((IEnumerable<IHandler<Command>>)container.Resolve(typeof(IEnumerable<IHandler<Command>>))).ToList();
        
        Assert.AreEqual(((CommandHandlerDecorator<Command>)commandHandlers[0]).Decorated.GetType(), typeof(CommandHandler1)); //fails, decorated is typeof(CommandHandler2)
        Assert.AreEqual(((CommandHandlerDecorator<Command>)commandHandlers[1]).Decorated.GetType(), typeof(CommandHandler2));

    }

    [Test]
    public void ResolveAllDecoratedCommandHandlers_Scanning_IsSucessful()
    {
        var builder = new ContainerBuilder();

        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .AsClosedTypesOf(typeof(IHandler<>))
            .AsImplementedInterfaces();

        builder.RegisterGenericDecorator(
            typeof(CommandHandlerDecorator<>),
            typeof(IHandler<>),
            context => context.ImplementationType.GetInterfaces().Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
        );

        var container = builder.Build();

        var commandHandlers = ((IEnumerable<IHandler<Command>>)container.Resolve(typeof(IEnumerable<IHandler<Command>>))).ToList();

        Assert.AreEqual(((CommandHandlerDecorator<Command>)commandHandlers[0]).Decorated.GetType(), typeof(CommandHandler1)); //fails, decorated is typeof(CommandHandler2)
        Assert.AreEqual(((CommandHandlerDecorator<Command>)commandHandlers[1]).Decorated.GetType(), typeof(CommandHandler2));

    }
}


interface IRequest { }
interface IHandler<in TRequest> where TRequest : IRequest { public void Handle(TRequest request); } 

interface ICommand: IRequest { }
interface ICommandHandler<in TCommand>: IHandler<TCommand> where TCommand: ICommand { } 

interface IQuery : IRequest { }
interface IQueryHandler<in TQuery>: IHandler<TQuery> where TQuery: IQuery { }

class Command : ICommand { }
class CommandHandler1 : ICommandHandler<Command> { public void Handle(Command command) => Console.WriteLine("CommandHandler1"); }
class CommandHandler2 : ICommandHandler<Command> { public void Handle(Command command) => Console.WriteLine("CommandHandler2"); }

class Query: IQuery { }
class QueryHandler1 : IQueryHandler<Query> { public void Handle(Query query) => Console.WriteLine("QueryHandler1"); }

class CommandHandlerDecorator<TCommand> : ICommandHandler<TCommand> where TCommand : ICommand
{
    public ICommandHandler<TCommand> Decorated { get; }
    public CommandHandlerDecorator(ICommandHandler<TCommand> decorated) => Decorated = decorated;
    public void Handle(TCommand request)
    {
        Console.WriteLine($"Command Decorator for {Decorated.GetType().FullName}");
        Decorated.Handle(request);
    }
}





