# TerroristChecker

API that provides checks of a person enlisted as terrorist using **fuzzy** string indexed search of it's name(s) by input string.

Fuzzy matching is implemented individualy for every word in input string using Dice coefficient algorithm, where words are splitted in n-grams and indexed. Full name may include one or more words. Full name match is calculated as an average of each name coefficient. In complex cases when several input words matches several person's names best match is calculated using Hungarian algorithm.

![diagram-export-14 06 2024-01_05_30](https://github.com/TimurRybakov/TerroristChecker/assets/69992861/19c010a4-1e62-4327-a391-28605895a3ab)

The solution uses docker compose for maintaining containers:

- Web app (ASP .NET Core 8)
- Database server (PostgreSQL 16.3)
- Database maintenance (PgAdmin 4)
- Response cache (Redis 7)

The solution is designed in the ideas of DDD and clean architecture. It uses MediatR ISender for CQRS pattern to execute commands for API and Integration tests under xUnit with Testcontainers.

Before statring the application ensure Docker desktop is installed and running.
