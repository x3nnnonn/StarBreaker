# StarBreaker

Extraction tool for Star Citizen game files.

## File extensions:

All of these files can be found when extracting Data.p4k from Star Citizen.
Some are common formats and immediately usable, other require conversion, and some are unknown.

### Usable

Files with these extensions can be opened with a program or converted to a more common format.
Some of them require specific parsing, which I've already done.

- dbc = StarBreaker.Forge
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
- cigvoxel
- cigvoxelheader
- dst

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
- vvg = vehicle voxel ?? sig 0xC? 0xBA 0xFE 0xCA
- topology = unknown. medical gown. starcloth? no signature
