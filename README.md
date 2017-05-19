doctest
========

> Test interactive F# examples.

[doctest][doctest] is a small program that checks examples in [XML Documentation][xml-doc].
It is similar to the [popular Haskell program with the same name][haskell].

## Building

In order to build doctest, ensure that you have [MSBuild][msbuild] and [NuGet][nugetdl] installed.

Clone a copy of the repo:

```
git clone https://github.com/moodmosaic/doctest
```

Change to the doctest directory:

```
cd doctest
```

Install all referenced packages:

```
nuget restore -PackagesDirectory packages
```

Build the project:

```
msbuild Doctest.fsproj /p:Configuration=Release
```

 [doctest]: https://github.com/moodmosaic/doctest
 [xml-doc]: https://docs.microsoft.com/en-us/dotnet/articles/fsharp/language-reference/xml-documentation
 [haskell]: http://hackage.haskell.org/package/doctest
 [msbuild]: https://github.com/Microsoft/msbuild
 [nugetdl]: https://dist.nuget.org/index.html