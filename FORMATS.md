# Formats

This is a list of file formats used in the game. The list is exhaustive, but not every format is known. The list is sorted by the amount of files in the game using that format.

| Amount | Extension       | Type                  | Format                      |
| ------ | --------------- | --------------------- | --------------------------- |
| 302693 | .dds            | Texture               | DXT                         |
| 192444 | .cgf            | Model                 | #ivo                        |
| 192181 | .cgfm           | Model                 | #ivo                        |
| 100120 | .wem            | Audio                 | Wwise                       |
| 50041  | .caf            | Animation?            | #ivo                        |
| 29150  | .cga            | Model                 | #ivo                        |
| 28778  | .cgam           | Model                 | #ivo                        |
| 22788  | .mtl            | Material              | CryXML                      |
| 15420  | .skinm          | Model                 | #ivo                        |
| 15420  | .skin           | Model                 | #ivo                        |
| 11427  | .xml            | XML                   | CryXML, sometimes plaintext |
| 8454   | .socpak         | Server obj. container | Zip                         |
| 3397   | .svg            | Vector                | SVG                         |
| 3275   | .chrparams      | Char Params           | CryXML                      |
| 2564   | .meshsetup      | Mesh Setup            | Plaintext                   |
| 1809   | .bnk            | Audio bank            | Wwise                       |
| 1409   | .adb            | animation db          | CryXML                      |
| 1205   | .animevents     | Anim Events           | CryXML                      |
| 950    | .cdf            | char definition       | CryXML                      |
| 950    | .bspace         | anim blend space      | CryXML                      |
| 590    | .opr            | object preset         | plaintext json              |
| 530    | .dba            | animation something   | #ivo                        |
| 518    | .aim            | animation ?           | #ivo                        |
| 422    | .chr            | model animation?      | #ivo                        |
| 330    | .swf            | Flash                 | Flash                       |
| 275    | .gfx            | Font                  | Flash                       |
| 216    | .bk2            | Video                 | Bink                        |
| 172    | .r16            | raw 16 bit            | Raw                         |
| 141    | .dst            | texture               | CryChunk                    |
| 131    | .eco            | planettech data       | plain json                  |
| 129    | .r8             | raw 8 bit             | Raw                         |
| 127    | .json           | json                  | plaintext json              |
| 121    | .vvg            | vehiche voxel         | Unknown                     |
| 113    | .txt            | misc engine configs   | plaintext                   |
| 79     | .comb           | CombinedBlendSpace    | CryXML                      |
| 77     | .RigLogic       | RigLogic              | RigLogic                    |
| 49     | .cfg            | engine config         | plaintext                   |
| 30     | .dna            | Dna                   | Dna format                  |
| 22     | .ttf            | TrueType Font         | TrueType                    |
| 20     | .cigvoxelheader | CIG Voxel Header      | CryChunk                    |
| 20     | .cigvoxel       | CIG Voxel             | CryChunk                    |
| 16     | .dst2           | dst2                  | CryChunk                    |
| 12     | .ini            | Localization          | plaintext                   |
| 9      | .png            | Image                 | PNG                         |
| 7      | .obj            | holoviewer model      | OBJ                         |
| 7      | .chf            | custom head file      | CHF                         |
| 6      | .raw            | unknown               | Raw                         |
| 6      | .dat            | unknown               | Raw                         |
| 5      | .pak            | shader cache          | Zip                         |
| 4      | .topology       | star cloth?           | unknown                     |
| 3      | .cax            | cax cache             | cax cache                   |
| 3      | .ogg            | Audio                 | Ogg                         |
| 2      | .TXT            | text                  | plaintext                   |
| 2      | .dm             | unknown               | unknown                     |
| 2      | .img            | animation something   | #ivo                        |
| 1      | .lut            | sky lut?              | SKY                         |
| 1      | .veg            | Vegetation            | CryXML                      |
| 1      | .dpl            | depletion             | plain json                  |
| 1      | .usm            | video?                | unknown                     |
| 1      | .dcb            | DataCore binary       | DataCoreBinary              |
| 1      | .pso            | compiled shader       | unknown                     |


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
