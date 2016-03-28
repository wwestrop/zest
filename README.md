#A exploration of code testability and state isolation

##Abstract
Automated tests are incredibly useful, and yet the tools and techniques that are required can cause your code to become less easy to understand, which in turn can hamper correctness. This post elaborates on some of my thoughts, and introduces an experimental piece of software that attempts to provide the best of both worlds. I’m a little dissatisfied with the status quo and would like to change the world, if that’s not too cheesy :-)

If this essay is too long, take a look at the [TLDR summary](#tldr) at the end.

##Introduction
What is the ideal kind of code, from a testing standpoint? From my perspective, this would be pure functional code. This means a function, which, when called, takes all the information it needs (its **state**) as arguments, and produces a return value. It does nothing else. It doesn’t update any other state or have any other side effects. 

This type of function is trivially easy to test. It also has the nice property of referential transparency (can be called with the same args as many times as you want and will always produce the same answer) - precisely because all the required state is provided by the caller and can be controlled. This property is especially desirable during testing because you may be calling your functions under test a great many times, and we do not want state leftover from one call to affect a call in a later test. 

However, languages and methodologies layer other state on top of this. For example, in object oriented (OO) languages, methods of a class have access to an object’s **instance state**, which could affect the outcome of the method. You also have application-level state (**statics**), and even state completely external to your application (files on disk, data from a network resource, content from a database, results from a webservice, and so on). All of these additional sources of state can affect the computation of a value, so are what we want to isolate during testing. 

For the purposes of this essay, I’ll class state into these types:

1. Function arguments
2. Instance variables
3. Static variables
4. External state

In the sections below, I’ll describe how these types of state are useful. Despite interfering with testability, the language’s designers included them for a reason, and it’s useful to see the trade-offs you have to make for a testable design, as then then we can begin to think how we might achieve the best of both worlds.

##Type 1 state - function arguments
Type 1 state, I think we can consider testable enough as-is. This does assume that the parameters passed are simple types. If they are reference types, they could have their own methods that could be called, and these calls could modify other state that could affect the outcome of later tests. (That’s a lot of “could”s). At this stage, I haven’t considered this angle, although if we can isolate the other sources of state, this may not be important. 

##Type 2 state - instance variables
Let us consider type 2 state, instance variables. These are often private and unavailable for callers to inspect or tamper with. This is **encapsulation**, an important part of the OO paradigm. When used as part of a codebase this can be a desirable property - the abstraction OO provides is that consumers of a class should not be concerned with (or indeed, tamper with) the inner workings of how a class completes its task, only that it does. Yet this exact property hampers testing. 

Commonly the reaction to this problem is that the class “does too much”. The solution given is to move private members into their own class, responsible for that single responsibility, and make them public. This new class with your previously-encapsulated code factored out is then injected to the original class as a dependency. 

In principle this is not a bad idea. However, in practice I find that perhaps the right level of granularity needs to be considered before doing this. If every class is responsible for **exactly** one single thing, surely each class will contain a single method, with a single line of code. Obviously, this would be unworkable, but illustrates that best practices can be taken too far. I feel, with all absolute rules, judgement gets left by the wayside (coding is part science, but also part art, after all). 

Overusing this technique creates a proliferation of trivial classes littering the public surface of your code which makes it harder for newcomers to comprehend, as there is an effect of making it “hard to see the wood for the trees”. Rather than scanning through a class and realising roughly what is going on, you may have to read and understand 10 others as well (the collaborators). When consuming this class, you will have to feed in all the dependencies you may know nothing about, and which are totally unrelated to the task at hand, increasing the amount you are required to mentally juggle. You get bogged down in details, which is one of the things encapsulation is supposed to protect you against (and if this piece of extracted logic has absolutely no use case outside of your class, it reduces cohesion as well). 

It also means nothing is truly abstracted as all the details are visible up front. 

##Type 3 state - statics
Let’s now investigate type 3 state, static variables. They are avoided in code which is to be unit tested; because the very property that their state persists for a long time makes things which are dependent on them hard to test in isolation (as state from one test can ‘leak’ into another accidentally and affect the test outcome).

However, I do believe statics have their place (otherwise the language designers would not have included them). In particular, they can better model certain aspects of the domain, as some things truly **are** static and only ever have one value. For example, the current date and time. At any given time, there will only ever be one true value. This value is changing all the time of course; but it can never be both 5:00 and 5:25 at the very same time.

Another example that I’ve also written was a license manager for a third party system which we integrated with. We had a shared pool of (expensive) licenses so it was crucial to use them as efficiently as possible. Therefore everything that wanted to talk to this system had **one place** to go to, which would properly ensure a license was available, and would deal with releasing it after a period of inactivity. A new license was instantiated only as often as required and since having a user consume two at once would be a waste, the variable holding the license was made static to prevent this (i.e. the business requirement was for a singleton). 

These problems are avoidable by passing the variable as a parameter instead of accessing them statically. But the point I’m making is that this adds extra things to the interface (and thus complicates it), for no other reason than to enable testing. 

This more complex interface gives a consuming developer more to think about, and for this reason can make coding errors more likely to creep in. Someone who doesn’t know a particular subsystem could easily pass in the wrong instance of a `DateTime` (perhaps they’re juggling a few and mix them up). Or they somehow manage to instantiate a `License` object from somewhere and pass that in. Code reviews help with this, but they aren’t infallible, and I fundamentally like the idea of code that is hard to get wrong in the first place, which guides consumers down the correct path. This is called **defensive coding**. Much like defensive driving, being prepared for the worst makes your software more robust. There are other practices to make code more defensive, but they don’t impact testing like statics do. 

Testing these scenarios is more difficult. I don’t disagree. But they compromise design and _I want to do better....._

##Type 4 state - external state
This is the big one, and it is typically the one we try and mock out when unit testing, because whereas using static or private instance variables can be avoided (despite both being useful tools); writing to a file, or calling a database or webservice is probably essential to the function of our application. 

However some core parts of the .NET Base Class Library (BCL) to do these things are not always written to be mockable, e.g. the filesystem manipulation classes. We could insert our own layer of indirection and consume this instead, an `IFileInfoFacade` or `IDirectoryInfoFacade` that merely defers to FileInfo at runtime yet can be mocked when testing. But because it’s not built in, it has to be repeated in every application you write (I would level a similar complaint at an `IDateTimeProvider`, which I see/use often). When picking up an unfamiliar project, you must hunt down the implementation of this facade (because they’re never named consistently), and this breaks your train of thought.

In this particular case, perhaps filesystem or database virtualisation could come to the rescue? But that seems like quite a heavyweight solution.  

To be honest I’ve mainly considered type 2 and 3 state in this project. In any case, type 4 state involves the things we typically focus on mocking out during testing anyway. Because of this I’m generally happy to take any hit to code readability. Things like the repository pattern are well understood, so despite being one more thing to juggle, a new developer will immediately have a clear understanding of what your `IWidgetRepository` is doing and why it’s required. 

It _may_ be possible to use a tool similar to the one I’m developing alongside this essay in order to overwrite built-in BCL behaviour such as file manipulation, `DateTime.Now`, and so on. The theory of this idea is to allow you to write code using the already existing, well-known, and widely-understood .NET classes (thus keeping your code clear and readable to newcomers), while having them transformed at test-time into something more mockable. But I haven’t investigated it here. It would involve transforming the BCL (automatically using a tool, but it still seems like a large undertaking). 

##What can be done?
Perhaps a new language? I’m not aware of any modern, widely-used language which has ease of testing as an explicit design goal, but do please let me know if there is one. Testing frameworks and tools usually seem “tacked on”. Every mocking framework I’ve used, for instance, required syntax that I find to be fussy and a bit disjointed.

Dependency Injection (DI) containers are another attempt at abstracting some of the extra complexity that we might have added to our public interfaces as outline above. I’ve never got on with them much, finding them to be a bit “magical”, another layer of indirection (or obfuscation) that’s great when it works, but incredibly painful when they don’t. I also prefer languages where the compiler catches as many errors as possible, as early as possible. If a DI framework (or rather, our configuration of it) is going to fail, it’s usually at runtime. Since we often configure the container differently at runtime than test time, it makes me uneasy to assume that they suitably replicate real-world execution conditions.

##Introducing Zest
I’ve started to work on a software framework, named “Zest”. It is an intermediary step that runs over your code to transforms it into a shape which is easier to test against. 

Before I go into details, please note a few points about Zest:

1. It was started with the intent of exploring Roslyn, and so I don’t have the best idea of how to accomplish things yet. What I want to accomplish may not be possible at all using this framework. The code is messy as I’m experimenting. 
2. It is not complete (in fact it is very **in**complete), and I don’t have much time at present to develop it. Further, there is no integration into the build pipeline, the tool must be invoked manually. 
3. The transformation is applied to the source code, which means if you link against a pre-built binary, Zest cannot help you. The .NET BCL is one such pre-built, as is such is a rather massive caveat at this stage :-( (although Microsoft does make the source code available these days)

With that out of the way, what features does Zest provide? (Or rather, what are the features that I envisage might be beneficial?)

* **Unsealing classes and methods**. This can help your code conform to and enforce domain requirements, however it inhibits testing. I’d like to be able to unseal members temporarily during testing.
* **Static**. I’d like Zest to be able to rewrite the use of statics so that they are treated as instances during testing-time (where they can have their values and behaviours mocked as appropriate, and where mutations to the object from one test are isolated from other tests), while at the same time still expressing this aspect of the domain when the code is written. 
* **Automatic virtualisation**. I’d like Zest to automatically make every member virtual, and thus amenable to being overridden at test time, without forcing you to make things virtual that your model doesn’t really warrant being virtual. 
* **Monkey patching**. This allows you to replace methods on an object completely. I find the syntax to be a lot clearer and more concise than the various mocking frameworks available. It also works on concrete, non-virtual methods. 
* **Encapsulation**. Oftentimes classes, methods, and members are exposed to test assemblies using `InternalsVisibleTo`. This basically breaks encapsulation, but only during testing. Zest could blow everything open at test time. _I’m in two minds about this and may never implement it. Testing advocates will tell you that you that a private member shouldn’t be tested, unless it is being consumed as some part of the public API. The reason is because it is simply an implementation detail that could change, and if it’s important enough to test, should be extracted into its own class. This makes sense, but see the section on [type2 state (instance variables)](#type-2-state---instance-variables) for why I don’t think this is a panacea._

##A few final points
I don’t expect Zest to be the solution. It exists more in order to start a discussion, and it is in a very rough state (Surprise! Talking about these problems is easier than fixing them). I feel a new language is needed (or substantial changes to an existing one), but I have no idea what this would look like. Finally, it was an opportunity for me to experiment with Roslyn. 

If anyone has a neat workaround for any of the pain points mentioned in this essay, do let me know!

##TLDR
Inversion of control and DI often leads to code that is more awkward to read and write. It should be said that I don’t have a problem with these concepts per-se, but with their **overuse**, or their use where they aren’t warranted (see appendix). 

Writing in this style can also render developers unable to use language features that might otherwise model the business requirements clearly and succinctly, or in a more defensive way with fewer moving parts to go wrong. 

I also want to make the point that simpler code is usually **better** code. 

At the same time, automated tests are a great boon and I would like to see them used more often. A great many legacy systems could severely do with a suite of automated tests, but the manner in which the system is coded does not lend itself to having its components isolated. Rewriting is almost never a viable option. 

This project is an attempt by myself to see if I can’t combine the best of both worlds. Straightforwardness of code, or testability? Why can’t we have both?

![Why can't we have both?](/docs/why-cant-we-have-both.jpg)

##Appendix
Just in case you aren’t convinced that dependency injection can be overused, here is a sample “hello world” application. 

Rather than mixing responsibilities, we have a class responsible for getting the user’s name, a class responsible for deciding on what greeting to present, and a class responsible for presenting this greeting somewhere. Each of these classes does exactly one thing, can be tested in isolation, and can be swapped out for other implementations; so it ticks a number of “best practices” boxes. 

Yet there's more indirection and boilerplate than actual code, and the simple task of this code is obscured. Furthermore, the likeliness of **actually** swapping out the dependencies for other implementations (in this case) is next to nil. But I see code like this all the time. 

```C#
/// <summary>
/// Outputs a greeting to the user
/// </summary>
interface IGreetingIssuer {
    void Greet();
}     
    
class SimpleGreetingIssuer : IGreetingIssuer {
    private readonly IMessageWriter _messageWriter;
    private readonly IGreetingProvider _greetingProvider;

    public SimpleGreetingIssuer(IMessageWriter messageWriter, IGreetingProvider greetingProvider) {
        if (messageWriter == null) {
            throw new ArgumentNullException(nameof(messageWriter));
        }
        _messageWriter = messageWriter;

        if (greetingProvider == null) {
            throw new ArgumentNullException(nameof(greetingProvider));
        }
        _greetingProvider = greetingProvider;
    }

    public void Greet() {
        string greeting = _greetingProvider.GetGreeting();
        _messageWriter.Write(greeting);
    }
}


/// <summary>
/// Responsible for determining the greeting to issue
/// </summary>
interface IGreetingProvider {
    string GetGreeting();
}

/// <summary>
/// Provides "Hello world" as a greeting
/// </summary>
class SimpleGreetingProvider : IGreetingProvider {
    public string GetGreeting() {
        return "Hello world";
    }
}

/// <summary>
/// Prompts the user for their name, and returns a string greeting them by name
/// </summary>
class NameGreetingProvider : IGreetingProvider {
    private readonly INameRetriever _nameRetriever;

    public NameGreetingProvider(INameRetriever nameRetriever) {
        if (nameRetriever == null) {
            throw new ArgumentNullException(nameof(nameRetriever));
        }
        _nameRetriever = nameRetriever;
    }

    public string GetGreeting() {
        string name = _nameRetriever.GetName();
        return $"Pleased to meet you, {name}";
    }
}



/// <summary>
/// Responsible for putting the greeting somewhere, once it's decided what the greeting is
/// </summary>
interface IMessageWriter {
    void Write(string message);
}

/// <summary>
/// Puts a greeting onto stdout
/// </summary>
class ConsoleMessageWriter : IMessageWriter {
    public void Write(string message) {
        Console.WriteLine(message);
    }
}



/// <summary>
/// Responsible for retrieving the user's name
/// </summary>
interface INameRetriever {
    string GetName();
}

/// <summary>
/// Retrieves the user's name by asking them for it from stdin
/// </summary>
class ConsoleNameRetriever : INameRetriever {
    private readonly IMessageWriter _messageWriter;

    /// <param name="messageWriter">For prompting the user for their name</param>
    public ConsoleNameRetriever(IMessageWriter messageWriter) {
        if(messageWriter == null) {
            throw new ArgumentNullException(nameof(messageWriter));
        }
        _messageWriter = messageWriter;
    }

    public string GetName() {            
        string name = "";
        while (name == "") {
            _messageWriter.Write("What is your name?");
            name = Console.ReadLine();
        }
        return name;
    }
}

/// <summary>
/// Retrieves the user's name by getting it from the currently logged-on user
/// </summary>
class EnvironmentNameRetriever : INameRetriever {
    public string GetName() {
        return Environment.UserName;
    }
}



class Program {
    static void Main(string[] args) {
        IGreetingIssuer greeter;

        // Simple hello world
        greeter = new SimpleGreetingIssuer(
            new ConsoleMessageWriter(),
            new SimpleGreetingProvider());
        greeter.Greet();

        // Greet the user by a name provided by them
        greeter = new SimpleGreetingIssuer(
            new ConsoleMessageWriter(),
            new NameGreetingProvider(
                new ConsoleNameRetriever(
                    new ConsoleMessageWriter())));
        greeter.Greet();

        // Greet the user by name gathered from the current logged-on session
        greeter = new SimpleGreetingIssuer(
            new ConsoleMessageWriter(),
            new NameGreetingProvider(
                new EnvironmentNameRetriever()));
        greeter.Greet();
    }
}
```

This is overkill in my opinion. Everything in moderation, including moderation itself. Don’t let the desire for clear code stop you from using DI, and don’t let a desire for testability make you go over-the-top with layers of indirection. This is an issue of judgement, so everyone will have a different opinion on how much is too much.
As David J. Wheeler once said, “All problems in computer science can be solved by another level of indirection... Except for the problem of too many layers of indirection”.
