## Requirements
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
Basic make commands are defined under `make/default.mk`. Each can be made
to apply to a specific project by adding `PROJECT_NAME=<name>` to the command.
These include:
- `all`: Builds everything using `dotnet` and Cake for debug and release.
- `clean`: Cleans everything using `dotnet` for debug and release.
- `rebuild`: Rebuilds everything using `dotnet` and Cake for debug and release.
- `install`: Installs the release build into the active Vintage Story installation.
- `uninstall`: Uninstalls the release build from the active Vintage Story installation.
- `run`: Launches Vintage Story with the debug build of the mod loaded in.

Advanced project commands are defined under `make/project.mk`. The basic
make commands are defined in terms of the more advanced commands. Some
commands do not have a basic equivalent such as `project-run-server`. Refer
to `make/project.mk` for more details.

## Future improvements
- Make it easier to launch Vintage Story with multiple development projects
loaded in at once using the `run` command.