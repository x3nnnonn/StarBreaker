# StarBreaker

Extraction and reverse-engineering tool for Star Citizen game files.

## Included parts

StarBreaker includes several projects, each useful to process a certain type of info or to perform a certain task

### StarBreaker
Avalonia UI that aims to be an easy way to view and extract things inside the p4k and datacore. It's very barebones right now, I wouldn't recommend using it.

### StarBreaker.Cli

Console application used mostly for development and reverse engineering purposes, has some useful commands to extract and process files. Here's the available functionality:
```sh
StarBreaker.Cli v1.0.0

USAGE
  StarBreaker.Cli [options]
  StarBreaker.Cli [command] [...]

OPTIONS
  -h|--help         Shows help text. 
  --version         Shows version information. 

COMMANDS
  chf-download      Downloads all characters from the website and saves them to the website characters folder. 
  chf-export-all    Exports all modded characters into the Star Citizen folder. 
  chf-export-watch  Watch for new modded characters and export them to the star citizen folder. 
  chf-import-all    Imports all non-modded characters exported from the game into our local characters folder. 
  chf-import-watch  Watch for new characters in the Star Citizen folder and import them. 
  chf-process       Process a character file 
  chf-process-all   Processes all characters in the given folder. 
  df-extract        Extracts a DataCore binary file into separate xml files 
  df-extract-single  Extracts a DataCore binary file into a single xml 
  p4k-extract       Extracts a Game.p4k file 

You can run `StarBreaker.Cli [command] --help` to show help on a specific command.
```
### StarBreaker.Chf - Character head files
Star Citizen can export and import custom characters via chf files. Some examples can be seen [here](https://www.star-citizen-characters.com/). The Chf project implements this format, being capable of reading, manipulating, and writing back the characters. This can allow for some things the game does not, be creative!

This project was the main reverse engineering work behind [starchar](https://github.com/diogotr7/starchar), which is a web application that allows for such modifications.

### StarBreaker.Protobuf & Grpc
The game communicates with the server via gRPC. With some gRPC clients (including the one the game uses), it is possible to grab protocol buffer [descriptors](https://protobuf.dev/reference/java/api-docs/com/google/protobuf/Descriptors.html) from the binary. This process is done in StarBreaker.Protobuf.

Next, we can write these descriptors to `*.proto` files, via some code I borrowed from [grpc-curl](https://github.com/xoofx/grpc-curl).

After that, we use the standard `Google.Protobuf` and `Grpc.Tools` packages in the `StarBreaker.Grpc` project to generate C# clients for these gRPC services.

This, along with some token grabbing (described in [GrpcClient.cs](src/StarBreaker.Protobuf/GrpcClient.cs)), can be used to make API calls to the game servers on your behalf.

#### mitmproxy
As an extra goodie for the gRPC communications, I've also implemented the necessary filtering for `mitmproxy` to properly inspect the packages [here](scripts/mitm.ps1), as well as the code to convert the gRPC buffers into human-readable json [here](src/StarBreaker.Protobuf/ReadAllBuffers.cs).

This can be used to inspect what the game is doing, like querying the entity graph for information.

### StarBreaker.Sandbox

Development project mostly so I can throw in some code and run it. Some miscellaneous things like runners for the rest of the code.

The one interesting part about this sandbox project is the [Crc32 brute forcing](src/StarBreaker.Sandbox/StringCrc32c.cs). Chf files (and possibly others, I'm not sure yet) save the crc32c hashes of strings as a uint32 to the file. It's surprisingly useful to know the original string that produced this hash, so I run the hashing function on every game-related string I can find (running `strings` on the exe, parsing the entire p4k and datacore, etc).

### P4k, DataCore, CryXml, CryChunk, Dds, etc
These projects are useful to extract data from the p4k file specifically. They implement the various custom formats the game uses to archive its assets. Below is a collapsed list of file types found in the p4k, and which ones are supported.

<details>

<summary>File Types</summary>

All of these files can be found when extracting Data.p4k from Star Citizen.
Some are common formats and immediately usable, other require conversion, and some are unknown.

### Usable

Files with these extensions can be opened with a program or converted to a more common format.
Some of them require specific parsing, which I've already done.

- dbc = StarBreaker.DataCore
- p4k = StarBreaker.P4k
- xml = StarBreaker.CryXmlB
- cfg = plain text, configuration
- chf = character head file, https://github.com/diogotr7/StarCitizenChf
- dpl = plaintext, json-ish. only one file. depletion?
- eco = plaintext, json-ish. planettech related. ecology?
- dds = texture, openable by many programs
- gfx = flash, use https://ruffle.rs/
- swf = flash, use https://ruffle.rs/
- ini  = plaintext, i18n
- json
- meshsetup = plain xml
- opr = plain json, object preset
- pak = zip file
- png = image
- svg
- ttf
- txt
- xml, sometimes
- bk2 = bink video
- bnk = wwise audio bank
- ogg = audio, openable by many programs
- obj = wavefront obj, 3d model
- usm = https://github.com/Rikux3/UsmToolkit
- wem = wwise audio

### CryXMLB

These files are CryXmlB files, which we can convert to regular xml.

- adb
- animevents
- bspace
- cdf
- cga
- chrparams
- comb
- mtl
- veg
- xml, sometimes

### CrChf

see cgf-converter, TODO.

- cga
- cgam
- cgf
- cgfm
- cigvoxel
- cigvoxelheader
- dst
- soc

### IVO

see cgf-converter, TODO.

- aim
- caf
- chr
- dba
- img
- skin
- skinm

### TODO / Unknown

Investigation needed. most of these are not obvious, not common, and probably not even very interesting (except socpak of course).

- cax = CAXCACHE, very uncommon
- dat = probably just misc data, might have to read header
- dna = DNA v1.6 signature, very interesting
- lut = only one file with header SKYL
- pso = directx pipeline state object?
- r16 = raw 16-bit ints? no clue. heightmaps or something?
- r8 = raw 8-bit ints? no clue. heightmaps or something?
- raw = no clue. from the path it seems to be planet texture related somehow
- RigLogic = RIG V1.9 signature. animation related?
- socpak - server object container pak. zip file. explore me
- vvg = vehicle voxel ?? sig 0xC? 0xBA 0xFE 0xCA | some of these are CrCh
- topology = unknown. medical gown. starcloth? no signature

</details>
