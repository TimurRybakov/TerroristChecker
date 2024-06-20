# TerroristChecker

API that provides checks of a person enlisted as terrorist using fuzzy string matching of it's name(s) with input string.

Fuzzy matching is implemented individualy for every word in input string using Dice coefficient algorithm, where words are splitted in n-grams. Full name may include one or more words. Full name match is calculated as an average of each name coefficient. In complext cases when several input words matches several person's names best match is calculated using Hungarian algorithm.


![diagram-export-14 06 2024-01_05_30](https://github.com/TimurRybakov/TerroristChecker/assets/69992861/19c010a4-1e62-4327-a391-28605895a3ab)
