# Clam

> A scaffolding system PoC for [Perla](https://github.com/AngelMunoz/Perla)

This is the Scaffolding PoC project for Perla, once this tool is refined a little bit more, the core pieces will be released as a scaffolding library.

Perla will be enhanced with it of course.

The idea is the following:

Adding repositores:

- Download Github Repo
- Save related data to the repository in a local database

Scaffolding New Projects:

- Call the new command
- Use the previously downloaded repositories
- Copy All the non `.tpl.` files
- Use F# Compiler services to evaluate scripts inside the template repositories to obtain any configuration object needed
- Compile the `.tpl.` files with Scriban and the configuration objects
- Call it a day

The Copy/Compilation structure/files/semantics/API is not yet defined but I'm grabbing inspiration from from https://github.com/makesjs/makes
