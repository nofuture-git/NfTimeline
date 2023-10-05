# NoFuture.Timeline

### Summary

This project is both an API for generating timeline diagrams (in text format) and an implementation of timelines for the history of Western Civilization (3000 BCE - 1992 CE).

---
### Overview

---
This project produces timeline documents in text format that appear a lot like "swim lanes" used in UML.  

The timelines require a fixed-width font such as Courier.

The original inspiration was from the book, _"A History of Israel" 4th ed. by John Bright_.  This book contained chronological charts that mapped time along the vertical axis and particular political polities as rectangles queued right to left.  This layout allows for illustrating political conflict (typically wars and battles) between the polities as arrows pointing from one block to another.  Here is an example from chart number 7 concerning the rise of the Roman Empire (325 BCE - 27 BCE).  

I created specific dotnet types for the various kinds of entries.  As you can see in the example below: territorial expansion is typically on the right and is prefixed with a '+' (e.g. `+Bithynia & Cyrene(76) `), political leaders are enclosed in square braces and also appear on the right (e.g.`[Antiochus VII 125-96]`), works of art and literature are typically in the middle in the form of title and author\artist (e.g. `'The Aeneid'Virgil(25)`), etc.

The code is made to auto-expand to fit the entries.  Each entry requires a date (or range of dates), and the code will automatically place the entry to match the vertical axis for the given date.  There are times when I had to tweek the entries to get them to all fit in particularly busy part of the chart.

```
      +-----------------------------------------------------------------------------------
      |                                                                                   
330   ||+--------------Roman-Republic--------------+|+----------Seleucid(Syria)---------+|
      ||                                            |                                    |
      ||                                            |                                    |
      ||                                            |                                    |
      ||                                            |[Seleucus I 312-280]                |
      ||                                            |                                    |
      ||                                            |                                    |
299   ||         Third Samnite War(298-290)         |                                    |
      ||                                            |                                    |
      ||                                            |                                    |
      ||                                            |                                    |
      ||+Greek Italy(283)      Pyrrhic War (280-279)|[Antiochus I 280-261]               |
      ||                                            |                                    |
      ||+Lombardy & lower Italy(272)                |                                    |
268   ||                                            |                                    |
      ||          First Punic War (264-241)         |                                    |
      ||                                            |[Antiochus II 261-246]              |
      ||                                            |                                    |
      ||                                            |                                    |
      ||           Hamilcar invades Sicily          |[Seleucus II 246-226]               |
      ||+Sicily(241)                                |                                    |
237   ||+Sardinia & Corsica(239)                    |         Parthian Empire begins(238)|
      ||                                            |                                    |
      ||                                            |[Seleucus III 226-223]              |
      ||+Cisalpine Gaul(222)                        |[Antiochus (the Great) III 223-187] |
      ||          Second Punic War(219-201)         |                                    |
      ||        First Macedonian War(214-205)       |                                    |
      ||         Rome capture Syracuse(212)         |                                    |
206   ||     Hasdrubal defeated @ Metaurus(207)     |                                    |
      ||     Scipio defeats Hannibal @ Zama(202)    |                                 ....
      ||+Spain(199)   Second Macedonian War(200-197)|                                    |
      ||                                            |                                    |
      ||                                         ......Treaty of Apamea....>             |
      ||                                            |[Seleucus IV 187-175]               |
      ||                                            |                                    |
175   ||                                            |[Antiochus IV (Epiphanes) 175-163]  |
      ||        Third Macedonian War(171-168)    .........................................
      ||+Greece(167)                                |                                    |
      ||                                            |[Antiochus V 163\162]               |
      ||                                            |[Dimetrius 162-150]                 |
      ||               [concrete](155)              |                                    |
      ||          Third Punic War(150-146)          |[Demetrius II 145-138]              |
144   ||+Tusinia(146)             Carthage destroyed|                                    |
      ||+Anatolia(138)        First Servile War(139)|                                    |
      ||                                            |                                    |
      ||                                            |                                    |
      ||                                            |[Antiochus VII 125-96]              |
      ||                                            |                                    |
      ||                                            |                                    |
113   ||         The Jugurthine War(112-105)        |                                    |
      ||+Allgeria(108)                              |                                    |
      ||         Second Servile War(103-99)         |                                    |
      ||          [arch bridge & damn](100)         |        [glass blowing](100)        |
      ||                                            |                                    |
      ||                                            |                                    |
      ||        First Mithridatic War(88-84)        |                                    |
82    ||        Second Mithridatic War(83-81)       |                                    |
      ||+Bithynia & Cyrene(76)                      |                                    |
      ||     Third Servile War[Spartacus](73-71)    |                                    |
      ||                                            |                                    |
      ||        Third Mithridatic War(75-63)     ..........>----------------------------+|
      ||  First Triumvirate[Caesar,Crassus,Pompey.........................................
      ||+Gaul(57)                                   |                                    |
51    ||  'Commentarii de Bello Gallico'Caesar(50)  |                                    |
      ||                                            |                                    |
      ||           Caesar assassinated(44)          |                                    |
      || Second Triumvirate[Antony,Octavian,Lepidus]|                                    |
      ||                                            |                                    |
      ||+Egypt(30)                               .........................................
      ||[Augustus 27-]        'The Aeneid'Virgil(25)|                                    |
20    ||+------------------------------------------+|                                    |

```

### How to use

To produce the timelines one need only to run all the unit tests `dotnet test`.  The resulting timelines will be deposited into your AppData folder under a child folder of `NoFuture\Timelines`.  Each chart will be duplicated as both a text document and as a PDF.

### Disclaimer

I created this project and its resulting timelines just for fun and, while I consider them accurate, they are in no way intended to operate as any kind of historic authority.  I am not a historian.

### History
This project was moved from my other repo, `31g` using the python tool `git-filter-repo` which allows for preserving the commit history of one repo into another.

### On going issues

Eventually I need to move the data out to an independent format - probably JSON, maybe YAML - instead of having the data embedded into the C Sharp code files.

The project still has a reference to iTextsharp which appears to only target the older .NET Frameworks.  Eventually it will need to be replaced with something that is specific to dotnet (a.k.a. dotnet core).