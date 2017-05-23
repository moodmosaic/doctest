doctest [![NuGet][nuget-shield]][nuget] [![Travis][travis-shield]][travis]
========

> Test interactive F# examples.

[Doctest][doctest] is a small program that checks examples in [XML Documentation][xml-doc].
It is similar to the [popular Haskell program with the same name][haskell].

## Example

Doctest was primarily created when porting Hedgehog's Range module [from Haskell][range-hs] [to F#][range-fs], so one possible example is to show Doctest in action:

```f#
(Excerpt from the Range.fs file.)

namespace Hedgehog

/// $setup
/// >>> let x = 3

/// A range describes the bounds of a number to generate, which may or may not
/// be dependent on a 'Size'.
type Range<'a> =
    | Range of ('a * (Size -> 'a * 'a))

module Range =
    ...

    //
    // Combinators - Constant
    //

    /// Construct a range which is unaffected by the size parameter with a
    /// origin point which may differ from the bounds.
    ///
    /// A range from @-10@ to @10@, with the origin at @0@:
    ///
    /// >>> Range.bounds x <| Range.constantFrom 0 (-10) 10
    /// (-10, 10)
    ///
    /// >>> Range.origin <| Range.constantFrom 0 (-10) 10
    /// 0
    ///
    /// A range from @1970@ to @2100@, with the origin at @2000@:
    ///
    /// >>> Range.bounds x <| Range.constantFrom 2000 1970 2100
    /// (1970, 2100)
    ///
    /// >>> Range.origin <| Range.constantFrom 2000 1970 2100
    /// 2000
    ///
    let constantFrom (z : 'a) (x : 'a) (y : 'a) : Range<'a> =
        Range (z, fun _ -> x, y)

    ...

    //
    // Combinators - Linear
    //

    [<AutoOpen>]
    module Internal =
        // The functions in this module where initially marked as internal
        // but then the F# compiler complained with the following message:
        //
        // The value 'linearFrom' was marked inline but its implementation
        // makes use of an internal or private function which is not
        // sufficiently accessible.

        /// Truncate a value so it stays within some range.
        ///
        /// >>> Range.Internal.clamp 5 10 15
        /// 10
        ///
        /// >>> Range.Internal.clamp 5 10 0
        /// 5
        ///
        let clamp (x : 'a) (y : 'a) (n : 'a) =
            if x > y then
                min x (max y n)
            else
                min y (max x n)

    ...

    /// Construct a range which scales the second bound relative to the size
    /// parameter.
    ///
    /// >>> Range.bounds 0 <| Range.linear 0 10
    /// (0, 0)
    ///
    /// >>> Range.bounds 50 <| Range.linear 0 10
    /// (0, 5)
    ///
    /// >>> Range.bounds 99 <| Range.linear 0 10
    /// (0, 10)
    ///
    let inline linear (x : 'a) : ('a -> Range<'a>) =
      linearFrom x x
```

To highlight what Doctest does when finding a failing case, we can go ahead and change some of the above tests on purpose:

```diff
diff --git a/src/Hedgehog/Range.fs b/src/Hedgehog/Range.fs
index 060f9f8..1ef4c2d 100644
--- a/src/Hedgehog/Range.fs
+++ b/src/Hedgehog/Range.fs
@@ -82,10 +82,10 @@ module Range =
     /// A range from @1970@ to @2100@, with the origin at @2000@:
     ///
     /// >>> Range.bounds x <| Range.constantFrom 2000 1970 2100
-    /// (1970, 2100)
+    /// (1970, 2101)
     ///
     /// >>> Range.origin <| Range.constantFrom 2000 1970 2100
-    /// 2000
+    /// 2001
     ///
     let constantFrom (z : 'a) (x : 'a) (y : 'a) : Range<'a> =
         Range (z, fun _ -> x, y)
@@ -137,7 +137,7 @@ module Range =
         /// Truncate a value so it stays within some range.
         ///
         /// >>> Range.Internal.clamp 5 10 15
-        /// 10
+        /// 101
         ///
         /// >>> Range.Internal.clamp 5 10 0
         /// 5
@@ -191,11 +191,14 @@ module Range =
     /// (0, 0)
     ///
     /// >>> Range.bounds 50 <| Range.linear 0 10
-    /// (0, 5)
+    /// (0, 51)
     ///
     /// >>> Range.bounds 99 <| Range.linear 0 10
     /// (0, 10)
     ///
+    /// >>> ([3; 2; 1; 0] |> List.map ((+) 1))
+    /// [1 + 3..1 + 0]
+    ///
     let inline linear (x : 'a) : ('a -> Range<'a>) =
       linearFrom x x
```

Compiling the above code and re-running Doctest will produce the following output:

```
(0, 51) = Range.bounds 50 <| Range.linear 0 10
Test failed:

(0, 51) = (0, 5)
false

[1 + 3..1 + 0] = ([3; 2; 1; 0] |> List.map ((+) 1))
Test failed:

[] = [4; 3; 2; 1]
false

(1970, 2101) = Range.bounds x <| Range.constantFrom 2000 1970 2100
Test failed:

(1970, 2101) = (1970, 2100)
false

2001 = Range.origin <| Range.constantFrom 2000 1970 2100
Test failed:

2001 = 2000
false

101 = Range.Internal.clamp 5 10 15
Test failed:

101 = 10
false
```

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

Build the project:

```
msbuild Doctest.fsproj /p:Configuration=Release
```

 [doctest]: https://github.com/moodmosaic/doctest
 [xml-doc]: https://docs.microsoft.com/en-us/dotnet/articles/fsharp/language-reference/xml-documentation
 [haskell]: http://hackage.haskell.org/package/doctest
 [msbuild]: https://github.com/Microsoft/msbuild
 [nugetdl]: https://dist.nuget.org/index.html

 [nuget]: https://www.nuget.org/packages/Doctest/
 [nuget-shield]: https://img.shields.io/nuget/dt/Doctest.svg?style=flat

 [travis]: https://travis-ci.org/moodmosaic/doctest
 [travis-shield]: https://travis-ci.org/moodmosaic/doctest.svg?branch=master

 [range-hs]: https://github.com/hedgehogqa/haskell-hedgehog/blob/d3e2d75e1141e2f734f3a037cd635946726b9003/hedgehog/src/Hedgehog/Range.hs
 [range-fs]: https://github.com/hedgehogqa/fsharp-hedgehog/blob/731290e713bf1790e0b388ddaae32d8bf93c83a0/src/Hedgehog/Range.fs