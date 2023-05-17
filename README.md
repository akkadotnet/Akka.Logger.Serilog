# Akka.Logger.Serilog

This is the Serilog integration plugin for Akka.NET. Please check out our [documentation](http://getakka.net/articles/utilities/serilog.html) on how to get the most out of this plugin.

Targets [Serilog 2.12.0](https://www.nuget.org/packages/Serilog/2.12.0).

### Semantic Logging Syntax
If you intend on using any of the Serilog semantic logging formats in your logging strings, __you need to use the SerilogLoggingAdapter__ inside your instrumented code or there could be elsewhere inside parts of your `ActorSystem`:

```csharp
var log = Context.GetLogger<SerilogLoggingAdapter>(); // correct
log.Info("My boss makes me use {semantic} logging", "semantic"); // serilog semantic logging format
```

or

```csharp
var log = MyActorSystem.GetLogger<SerilogLoggingAdapter>(myContextObject); // correct
log.Info("My boss makes me use {semantic} logging", "semantic"); // serilog semantic logging format
```

or
```csharp
var log = MyActorSystem.GetLogger<SerilogLoggingAdapter>(contextName, contextType); // correct
log.Info("My boss makes me use {semantic} logging", "semantic"); // serilog semantic logging format
```

This will allow all logging events to be consumed anywhere inside the `ActorSystem`, including places like the Akka.NET TestKit, without throwing `FormatException`s when they encounter semantic logging syntax outside of the `SerilogLogger`.

### Adding Property Enricher To Your Logs

#### Default Properties
You can add property enrichers to the logging adapter that will be added to all logging calls to that logging adapter.

```csharp
var log = Context.GetLogger<SerilogLoggingAdapter>()
    .ForContext("Address", "No. 4 Privet Drive")
    .ForContext("Town", "Little Whinging")
    .ForContext("County", "Surrey")
    .ForContext("Country", "England");
log.Info("My boss makes me use {Semantic} logging", "semantic");
```

All logging done using the `log` `ILoggingAdapter` instance will append "Address", "Town", "County", and "Country" properties into the Serilog log.

#### One-off Properties

You can add one-off property to a single log message by appending `PropertyEnricher` instances at the end of your logging calls.

```csharp
var log = Context.GetLogger<SerilogLoggingAdapter>();
log.Info(
    "My boss makes me use {Semantic} logging", "semantic",
    new PropertyEnricher("County", "Surrey"), 
    new PropertyEnricher("Country", "England"));
```

This log entry will have "County" and "Country" properties added to it.

## Building this solution
To run the build script associated with this solution, execute the following:

**Windows**
```
c:\> build.cmd all
```

**Linux / OS X**
```
c:\> build.sh all
```

If you need any information on the supported commands, please execute the `build.[cmd|sh] help` command.

This build script is powered by [FAKE](https://fake.build/); please see their API documentation should you need to make any changes to the [`build.fsx`](build.fsx) file.

### Conventions
The attached build script will automatically do the following based on the conventions of the project names added to this project:

* Any project name ending with `.Tests` will automatically be treated as a [XUnit2](https://xunit.github.io/) project and will be included during the test stages of this build script;
* Any project name ending with `.Tests` will automatically be treated as a [NBench](https://github.com/petabridge/NBench) project and will be included during the test stages of this build script; and
* Any project meeting neither of these conventions will be treated as a NuGet packaging target and its `.nupkg` file will automatically be placed in the `bin\nuget` folder upon running the `build.[cmd|sh] all` command.

### DocFx for Documentation
This solution also supports [DocFx](http://dotnet.github.io/docfx/) for generating both API documentation and articles to describe the behavior, output, and usages of your project. 

All of the relevant articles you wish to write should be added to the `/docs/articles/` folder and any API documentation you might need will also appear there.

All of the documentation will be statically generated and the output will be placed in the `/docs/_site/` folder. 

#### Previewing Documentation
To preview the documentation for this project, execute the following command at the root of this folder:

```
C:\> serve-docs.cmd
```

This will use the built-in `docfx.console` binary that is installed as part of the NuGet restore process from executing any of the usual `build.cmd` or `build.sh` steps to preview the fully-rendered documentation. For best results, do this immediately after calling `build.cmd buildRelease`.

### Release Notes, Version Numbers, Etc
This project will automatically populate its release notes in all of its modules via the entries written inside [`RELEASE_NOTES.md`](RELEASE_NOTES.md) and will automatically update the versions of all assemblies and NuGet packages via the metadata included inside [`common.props`](src/common.props).

If you add any new projects to the solution created with this template, be sure to add the following line to each one of them in order to ensure that you can take advantage of `common.props` for standardization purposes:

```
<Import Project="..\common.props" />
```
