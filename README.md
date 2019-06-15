# Roentgenium - Rg - The Random Generator

## Motivation

A need arose for large, random-but-real-looking data sets and like any proper software developer I immediately took things too far. I also identified - and took advantage of - an opprotunity to learn a lot more about C#'s impressive reflective capabilites.

As for the name, I'm naturally terrible at naming things so I started simply with just "Random Generator". That shortened into "Rg" which, thanks to high school chemistry, I'd recalled was a chemical symbol. And that was that.

Implemented as a [.NET Core](https://docs.microsoft.com/en-us/dotnet/core/) [REST API](https://docs.microsoft.com/en-us/aspnet/core/mvc/overview?view=aspnetcore-2.2#web-apis), `Rg` can be built & run on any major modern OS.

## Building

`dotnet build` in the project directory (the same as in which `Roentgenium.csproj` lives)

### Prerequisities

* [.NET Core 2.2](https://dotnet.microsoft.com/download/dotnet-core/2.2) or later

### Optional

* [Microsoft Azure](https://azure.microsoft.com/) account:
    * [KeyVault](https://docs.microsoft.com/en-us/azure/key-vault/) can be used to store secrets (such as [connection strings](https://github.com/rpj/rg/blob/master/appsettings.Production.WithAzure.json#L26-L30))
    * [Blob storage](https://azure.microsoft.com/en-us/services/storage/blobs/) can be [used](https://github.com/rpj/rg/blob/master/appsettings.Production.WithAzure.json#L31-L34) to store the resulting artifacts
* A Redis instance:
    * To use the [`stream` output format](#stream-output-format)

## Running

`dotnet run` in the project directory
    * the `ASPNETCORE_ENVIRONMENT` environment variable *directly* controls (via simple substituion) which `appsettings.*.json` file is used *via the interpolation `appsettings.{ASPNETCORE_ENVIRONMENT}.json`).

In the simplest, default mode, generated data sets will be persisted *only* via the [Filesystem](https://github.com/rpj/rg/blob/master/Stages/Persistence/FilesystemPersistence.cs) persistence module with the artifacts written into the working directory.

### In "Production"

The build artifacts (`Roentgenium.dll` and its brethen in `bin/{CONFIG}/netcoreapp2.2`) are relocatable and can be run directly via the `dotnet` tool by *eliding* (oddly enough) the `run` verb and directly specifiying the `dll` path itself:

* `dotnet bin/Release/netcoreapp2.2/Roentgenium.dll`

`Rg` will always look in the working directory for the appropriate `appsettings` file, so if run directly from the `bin/Release/netcoreapp2.2/` without any settings files, the default configuration (as noted above) will be used.

## Using

For interface documentation, `Rg` includes [Swagger](https://swagger.io/) self-description support, always accessible on any running instance via the [`/swagger`](http://rg.rpjios.com/swagger) path.

[Postman](https://www.getpostman.com/) is the recommend way to interact easily with the interface.

### `stream` output format

This output format is implementation-specific to `Rg`, utilizing [Redis](https://redis.io/) [pub/sub](https://redis.io/topics/pubsub) to stream generated data to any number of interested subscribers.

It requires that the [`Extra`](https://github.com/rpj/rg/blob/master/General/Config.cs#L52-L56) field of the generator configuration structure include an entry [named `streamId`](https://github.com/rpj/rg/blob/master/Stages/Sinks/StreamSink.cs#L19-L25), which specifies the channel name to be used when [publishing](https://redis.io/commands/publish) [each record](https://github.com/rpj/rg/blob/master/Stages/Sinks/StreamSink.cs#L47).

### Demos

* [`rg.rpjios.com`](http://rg.rpjios.com/info) allows larger data sets but does not persist anything to Azure or *currently in any way that is retrievable by the end user!* Best for playing with the [convenience method](https://github.com/rpj/rg/blob/master/Controllers/Generate.cs#L172-L209).
* [`azure.rg.rpjios.com`](http://azure.rg.rpjios.com/info) only allows small data sets but *does* persist to Azure (per [this configuration](https://github.com/rpj/rg/blob/master/appsettings.Production.WithAzure.json#L25-L36)).

## Extending

There are many points of extensibility in `Rg` and developers wishing to extend its functionality are encouraged to do so and submit [a PR](https://github.com/rpj/rg/pulls) any time.

### Specifications

Living [here](https://github.com/rpj/rg/tree/master/Specifications), they're simple serializable classes
implementing [`ISpecification`](https://github.com/rpj/rg/blob/master/General/Types.cs#L80) which [are then
exposed](https://github.com/rpj/rg/blob/master/General/BuiltIns.cs#L43-L50) as the [supported specifications](http://rg.rpjios.com/info/supported/specifications).

### Field generators

Fields in an `ISpecification` are generated based on either the [default generator for the field's `Type`](https://github.com/rpj/rg/blob/master/FieldGenerators/DefaultGenerators.cs) or a [custom generator](https://github.com/rpj/rg/blob/master/FieldGenerators/CustomGenerators.cs) specified explicitly per-field via the [`GeneratorTypeAttribute`](https://github.com/rpj/rg/blob/master/General/Attrs.cs#L61-L74).

### Stages

The general [`IPipeline`](https://github.com/rpj/rg/blob/master/General/Types.cs#L28-L65) interface specifies a feed-forward data pipeline, currently concretely implemented only once by [`Pipeline.cs`](https://github.com/rpj/rg/blob/master/Pipeline/Pipeline.cs).

#### Sources

[`ISourceStage`](https://github.com/rpj/rg/blob/master/General/Types.cs#L82-L100) implementation. There currently is [only one](https://github.com/rpj/rg/blob/master/Stages/Sources/GeneratorSource.cs) which generates random data based on the specification & any mutating [attributes](https://github.com/rpj/rg/blob/master/General/Attrs.cs) applied, eventually calling the appropriate [field generators](https://github.com/rpj/rg/tree/master/FieldGenerators) to build data sets.

However, the source interface only requires implementation of [a single method](https://github.com/rpj/rg/blob/master/General/Types.cs#L88-L99), so adding different sources would be relatively straightforward though would require [addressing](https://github.com/rpj/rg/blob/master/General/Types.cs#L37) [a](https://github.com/rpj/rg/blob/master/General/Types.cs#L83-L84) [few](https://github.com/rpj/rg/blob/master/Pipeline/Pipeline.cs#L77-L78) [assumptions](https://github.com/rpj/rg/blob/master/Pipeline/Pipeline.cs#L135-L136) that there'd only ever be one.

Of note is that the system assumes that any concrete implemenation is capable of [producing infinite `IGeneratedRecord`s](https://github.com/rpj/rg/blob/master/General/Types.cs#L89-L91).

#### Intermediates ("filters")

[`IIntermediateStage`](https://github.com/rpj/rg/blob/master/General/Types.cs#L133-L139) implementations which themselves are just both an `ISourceStage` & `ISinkStage` at once, having each record "passed through" during execution of the overall pipeline. They are [enumerated at runtime](https://github.com/rpj/rg/blob/master/General/BuiltIns.cs#L52-L56) to be exposed as the [supported filters](http://rg.rpjios.com/info/supported/filters).

#### Outputs

[`ISinkStage`](https://github.com/rpj/rg/blob/master/General/Types.cs#L104-L131) implementations named according to and with an [`OutputFormatSinkType`](https://github.com/rpj/rg/blob/master/General/Attrs.cs#L37-L42) attribute specified, these stages are [exposed at runtime](https://github.com/rpj/rg/blob/master/General/BuiltIns.cs#L64-L65) as the [available output formats](http://rg.rpjios.com/info/supported/outputs).

The [`stream`](https://github.com/rpj/rg/blob/master/Stages/Sinks/StreamSink.cs) format implementation does not use the bonafide [`stream`](https://redis.io/topics/streams-intro) data type as it isn't yet widely available.

#### Persistence

Implementations of [`IPersistenceStage`](https://github.com/rpj/rg/blob/master/General/Types.cs#L141-L150), a specialized stage that exists only to persist the otherwise-ephermal results of the pipeline somewhere else.
