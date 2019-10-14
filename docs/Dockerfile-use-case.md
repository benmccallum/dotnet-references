# Dockerfile use case

Writing effecient Dockerfiles is important to keep us productive, but Docker doesn't support globbing in their COPY command. 
As such, the community has had a fun time trying to work around this limitation as nearly every programming stack and/or package manager
typically relies on a directory structure for determining dependencies and restoring them. 

Andrew Lock's blog posts show his progression through this challenge and with the help of some readers he finally settled a few approaches documented 
[here](https://andrewlock.net/optimising-asp-net-core-apps-in-docker-avoiding-manually-copying-csproj-files-part-2/).

Here's another approach, taking advantage of the fact that dotnet core global tools are cross-platform and (in my case anyway) we've already got dotnet core installed in the base Docker image.

## Setup

### Install the tool
First, install this tool by adding the following line to your Dockerfile.
Note: it's probably a good idea to pin to a specific version to avoid breaking changes.

`RUN dotnet tool install -g dotnet-fix-references --version 0.0.9`

If you're using a Linux image, you'll need to then add the following to make the tool available to be called directly:

`ENV PATH="${PATH}:/root/.dotnet/tools"`

Next, copy over a .sln file that serves as an "entry point" for tracking down the rest of your .csproj files. It *must* be copied into the correct location relative to the "root".

### Prime the files
`COPY MyCompany.MySolution.sln ./The/Correct/Directory/Path/`

> (Alternatively, you can use a .csproj as an entry, coming soon...)

Copy over your .csproj files using whatever technique works best for your situation.

```
# This results in a layer for each step
COPY *.csproj .
COPY */*.csproj .
COPY some-other-dir/*/*.csproj .

# This is one step but will find all of them without remorse (that aren't .dockerignore'd)
RUN for file in $(ls *.csproj); do mv $file .; done
# TODO: Windows version...

# Or some combination of the above
```

### Run the tool
Run this dotnet global tool at your sln file (or csproj file), 
give it the "root" where all the .csproj files were copied, and
optionally passing `true` to delete .csproj files that aren't actuallly in the dependency graph from the entry point.
> dotnet fix-references ./The/Correct/Directory/Path/MyCompany.MySolution.sln . true

You can run a command like so to validate the files are now in the right structure, ready for a NuGet restore to be fired.

`RUN ls  . -alR`
(todo, windows cmd)

### Complete your Dockerfile

You'll now be able to run a `restore` against the same entry point, which Docker will cache.

Then just complete your Dockerfile as per usual, copying over the rest of your src, doing your `build`, `test`, `publish`, etc. as needed.

## Closing remarks

I hope you've found this helpful and you've been able to progress through what's quite a frustrating functional hole in Dockerfile builds. BuildKit is on the way, hopefully that alleviates so much hoop jumping.

Thanks to Andrew Lock and his readers for sharing their approaches with the community. 
