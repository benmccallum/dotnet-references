# Dockerfile use case

Writing effecient Dockerfiles is important to keep us productive, but Docker doesn't support globbing in their COPY command. 
As such, the community has had a fun time trying to work 
around this limitation as nearly every programming stack an/or package manager
typically relies on a directory structure for determing dependencies and restoring them. 

Andrew Lock's blog posts show his progression through this challenge and he finally settled on the approaches documented 
[here](https://andrewlock.net/optimising-asp-net-core-apps-in-docker-avoiding-manually-copying-csproj-files-part-2/).

Here's another approach, taking advantage of the fact that dotnet global tools are cross-platform.

First, install this tool by adding the following line to your Dockerfile. 
Note: it's probably a good idea to pin to a specific version to avoid breaking changes.

> ...

If you're using a Linux image, you'll need to then add the following to make the tool available to be called directly:

> ...

Next, copy over a .sln file that serves as an "entry point" for tracking down the rest of your .csproj files.
> 

(Alternatively, you can use a .csproj as an entry, coming soon...)

Copy over your .csproj files using whatever technique works best for your situation.

```
# This results in a layer for each step
COPY *.csproj .
COPY */*.csproj .
COPY some-other-dir/*/*.csproj .

# This is one step but will find all of them without remorse (that aren't .dockerignore'd)
RUN for file in $(ls *.csproj); do mv $file .; done
# TODO: Windows version...
```

Run this dotnet global tool at your sln file (or csproj file), 
optionally passing `true` to delete .csproj files that aren't actuallly in the dependency graph from the entry point.
> dotnet fix-references MyCompany.MySolution.sln . true

Continue on as usual, restoring from your entry point, building, running tests, publishing, etc.
