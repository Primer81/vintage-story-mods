![Vintage Story Mods](img/Misc/VintageStoryModsLogoProfilePicTransparent.png)

## Build Requirements
- Vintage Story installed
- `dotnet` command line tool
- `python` command line tool
- `make` command line tool
- Basic CLI commands are necessary as well including:
    - `cp`
    - `rm`
    - `touch`
    - `mkdir`

## Make commands
Basic `make` commands are defined under `make/default.mk`. Each can be made
to apply to a specific set of projects by adding `PROJECT_NAME_LIST=<names...>`
before the command. These include:
- `all`: Builds everything using `dotnet` and Cake for debug and release.
- `clean`: Cleans everything using `dotnet` for debug and release.
- `rebuild`: Rebuilds everything using `dotnet` and Cake for debug and release.
- `install`: Installs the release build into the active Vintage Story installation.
- `uninstall`: Uninstalls the release build from the active Vintage Story installation.
- `name="<project-name...>"`: Add prior to any other command to specify which projects to target. Defaults to a list containing all mod projects under `src` except `DataDumper`.

The following `make` commands can only be used with one project at a time
which can be specified by adding `PROJECT_NAME=<name>` before the command
- `run`: Launches Vintage Story with the debug build of the mod loaded in.
- `name="<project-name>"`: Add prior to any other command to specify which project to target. Defaults to `DataDumper`.

If any of the specified project names do not yet exist, they will be created
automatically under the `src` directory.

Other basic `make` commands include:
- `decompile`: Decompiles the active vintage story installation DLL's into the `references/new` directory using `ilspycmd`.

Advanced project commands are defined under `make/project.mk`. The basic
`make` commands are defined in terms of the more advanced commands. Some
commands do not have a basic equivalent such as `project-run-server`. Refer
to `make/project.mk` for more details.

## Future improvements
- Make it easier to launch Vintage Story with multiple development projects
loaded in at once using the `run` command.