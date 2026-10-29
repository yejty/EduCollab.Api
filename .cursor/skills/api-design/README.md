# REST API Design

## Hlavní princip

Dobré REST API vzniká návrhem, ne postupným přidáváním endpointů podle aktuální potřeby implementace. API je kontrakt pro klienty, a proto se špatně mění, jakmile se na něj někdo napojí.

REST sám o sobě neříká nic o tom, jestli je konkrétní API kvalitní. Kvalitu dělá až promyšlený kontrakt, konzistence a použitelnost pro klienty.

Pro veřejná nebo integrační API je vhodný **design-first** nebo **specification-first** přístup:

- nejdříve sesbírat požadavky od aktérů integrace,
- navrhnout zdroje, operace, dotazy, reprezentace a chybové stavy jako jeden celek,
- specifikaci použít pro dokumentaci, mockování, testování i implementaci,
- specifikaci použít jako vstup pro toolchain kolem API,
- specifikaci brát jako závazné místo pravdy pro tým i klienty,
- umožnit paralelní vývoj klienta a serveru proti stejnému kontraktu,
- vyhnout se tomu, aby API bylo jen odrazem databázových tabulek s CRUD operacemi.

Dobře navržená OpenAPI specifikace se dá použít i jako vstup pro generování klientů, serverové kostry a testů pomocí AI nástrojů.

## Nástroje a specifikace

Pro návrh API lze použít nástroje jako Apidog:

- umožňuje definovat opakovaně použitelná schémata,
- podporuje verzování a generování dokumentace,
- dovoluje přidat vlastní Markdown dokumentaci, například quickstarty,
- umí publikovat API referenci,
- podporuje lokální i cloudové mock servery,
- dovoluje definovat konkrétní mock expectations pro speciální scénáře.

Mock server pomáhá odhalit chyby v návrhu dříve, než API začne být používáno v produkci.

## Základní URL struktura

Obvyklé rozdělení:

- `website.cz` pro hlavní web,
- `api.website.cz` pro API,
- případně `website.cz/api` pro API, které slouží jen jako backend konkrétní webové aplikace,
- `auth.website.cz` pro autentizační server,
- `developer.website.cz`, `developer.website.cz/docs` nebo `developer.website.cz/docs/reference` pro vývojářský portál a dokumentaci.

Za doménou může být segment pro publikum a verzi API:

```text
api.website.cz/public/v1/customers
```

Segment jako `public` odděluje veřejné API od jiných rozhraní. Segment `v1` dává prostor pro budoucí breaking verzi, i když hlavní cíl je verzování co nejdéle nepotřebovat.

Cesta za doménou je technicky case-sensitive, ale API by nemělo zbytečně trestat rozdíl ve velikosti písmen u běžných resource segmentů. Pozor ale na identifikátory, protože některá externí ID mohou být case-sensitive.

## Resource-Based URL

REST API má být postavené nad zdroji. Resource se obvykle pojmenovává podstatným jménem v množném čísle:

- `users`
- `customers`
- `invoices`
- `products`

Základní pravidlo:

- kolekce: `resources`
- instance: `resources/{id}`
- podřízený resource jen tehdy, když je skutečně potřeba pro identifikaci nebo kontext.

URL má jednoznačně a unikátně identifikovat konkrétní množinu dat.

Příklad přirozené struktury:

```text
e-shops/{eShopId}/products/{productId}/categories/{categoryId}
```

Takto hluboké vnoření ale často zbytečně komplikuje použití. Pokud je možné získat produkt podle jeho vlastního ID, není vhodné nutit klienta znát i `eShopId`.

Preferujte kratší URL, pokud stále jednoznačně identifikují resource.

## Nevhodné URL vzory

Nevhodné příklady:

```text
user/{id}
users/invoices
users/{id}/name
users/{id}/disable
articles/{id}/publish
accounts/{id}/activate
```

Důvody:

- `user/{id}` používá jednotné číslo místo množného,
- `users/invoices` neříká jednoznačně, jaký vztah mezi uživateli a fakturami vyjadřuje,
- `users/{id}/name` dělá endpoint pro jednotlivou property,
- `disable`, `publish` nebo `activate` jsou akce vepsané do URL, zatímco manipulace má probíhat přes HTTP metody a resource reprezentace.

## Subresources a filtrování

Na první pohled může vypadat správně:

```text
customers/{customerId}/invoices
orders/{orderId}/invoices
```

Problém vznikne ve chvíli, kdy přibudou další filtry, například datum, zákazník i objednávka současně. Potom je nutné stejné filtrování implementovat na více endpointech.

Vhodnější je obecný resource a query parametry:

```text
invoices
invoices?customerId={customerId}
invoices?orderId={orderId}
invoices?customerId={customerId}&orderId={orderId}
```

URL má identifikovat resource. Filtrování patří do query parametrů.

## Vztahy 1:1

U vztahů 1:1 není nutné tvořit vnořené field-like cesty:

```text
users/{id}/profile
users/{id}/image
users/{id}/settings
```

Lepší je vytvořit samostatný pseudoresource:

```text
user-profiles/{id}
user-images/{id}
user-settings/{id}
```

Pseudoresource nemusí odpovídat samostatné databázové tabulce. Je to veřejný kontrakt API, ne nutně kopie databázového modelu.

## Identifikátory

Identifikátor resource patří vždy do URL path.

`customers/{id}` má vrátit:

- přesně jednoho zákazníka,
- nebo `404 Not Found`, pokud neexistuje.

Není vhodné modelovat single-resource lookup jako:

```text
customers?id={id}
```

`customers` je kolekce, takže by takový endpoint formálně vracel kolekci s nulou nebo jedním prvkem. To je nesystémové a horší pro obvyklé cache strategie.

### Volba ID

Možnosti a rizika:

- Interní integer ID je jednoduché a efektivní, ale predikovatelné a vystavuje interní identifikátor navenek.
- E-mail nebo jiné měnitelné údaje nejsou vhodné, protože ID má být stabilní.
- GUID je stabilnější a méně predikovatelný, ale delší a méně výkonný.
- GUID v7 je vhodnější než náhodný GUID v4, protože obsahuje časovou složku a lépe se indexuje.
- Krátké custom identifikátory mohou být použitelné, ale obvykle vyžadují mapování nebo samostatný index.

Identifikátor v API se často stává `externalId` v systémech klientů. Neměl by se měnit.

Jednodušší je, když server identifikátor vytvoří sám a vrátí ho v odpovědi. Pokud ID vytváří klient, je nutné jasně definovat, kdy je ID přijato, kdy koliduje a jak se chová opakovaný požadavek.

## Query Design

Při návrhu list endpointů je nutné zjistit skutečné scénáře filtrování, řazení a stránkování. Klienti často požadují filtrování a řazení podle všech polí, ale reálné use casy bývají užší.

Každý list endpoint by měl mít:

- jasná výchozí pravidla,
- deterministické řazení,
- definovanou výchozí velikost stránky,
- maximální velikost stránky,
- validaci povolených sort fieldů,
- validaci formátu filtrů,
- dokumentované chování pro neplatné kombinace parametrů.

Výchozí dataset má odpovídat nejčastějšímu použití. Například `products` by typicky nemělo vracet smazané produkty, pokud to není hlavní use case.

Typické implicitní pravidlo může být: první stránka, prvních 10 položek, pouze neodstraněné záznamy a řazení podle ID nebo jiného stabilního klíče.

Výchozí a maximální velikost stránky je potřeba volit podle reálných scénářů. Příliš velká stránka zbytečně zatěžuje síť a server. Příliš malá stránka vede k tomu, že klient pošle několik requestů najednou, jen aby naplnil jeden grid nebo obrazovku.

### Striktní validace

Je lepší vrátit chybu při neplatném požadavku než tiše upravovat chování.

Příklad: pokud API podporuje maximálně `pageSize=100` a klient pošle `pageSize=500`, má dostat chybu. Pokud server tiše vrátí 100 položek, klient si může vybudovat špatné očekávání a pozdější změna chování ho rozbije.

## Semantika query parametrů

Operátor `=` by měl znamenat rovnost.

Pokud je potřeba jiná semantika, má být explicitní:

```text
location=Prague
locationContains=Prague
locationStartsWith=Pra
publishedFrom=2025-01-01
publishedTo=2025-12-31
```

Není vhodné schovávat operátor do hodnoty parametru, protože se hůře validuje ve specifikaci.

Názvy a suffixy musí být konzistentní napříč API. Pokud `createdFrom` na jednom endpointu znamená včetně krajní hodnoty, má to stejně znamenat i jinde.

Rozlišujte datum a časový okamžik už v názvu. Například `published` nebo `publishedDate` typicky evokuje datum, zatímco `publishedAt` říká, že hodnota obsahuje i časovou složku.

Pokud začne být filtrování natolik obecné, že klient potřebuje operátory nad téměř každou property, REST query model se rychle komplikuje. V takovém případě může být vhodnější zvážit jiný dotazovací model, například GraphQL, místo vymýšlení stále složitějších query parametrů.

## Fields, Expand a Granularita

Parametry typu `fields` nebo `expand` mohou narušovat cache a komplikovat kontrakt, protože jedna URL může vracet různé tvary dat. Pokud je API navržené jako univerzální, mělo by být spíše granulární:

```text
products?expand=categories
products?fields=id,name
```

- endpoint pro fakturu vrací fakturu,
- související data se získají z vlastních endpointů,
- klient si data skládá podle potřeby.

Granulární API může vést k většímu počtu requestů, například `1 + n` dotazům. To je běžná vlastnost REST API a má se řešit cacheováním, škálováním a případně cíleným redesignem podle telemetry.

## Unified Resource

Pro jeden resource je vhodné používat stejnou reprezentaci pro:

- `GET resources/{id}`,
- úspěšný `POST`,
- úspěšný `PUT`,
- úspěšný `PATCH`.

Nevhodné je vracet jednu strukturu z `GET` a jinou z `POST`, nebo canonical resource balit do zbytečných envelope objektů:

```json
{
  "article": {
    "id": "123",
    "title": "..."
  }
}
```

Pokud API nemá jednotnou envelope konvenci, vracejte přímo resource.

Canonical reprezentace by neměla obsahovat velké nested objekty, pokud stačí ID souvisejících zdrojů. Příliš vnořená data komplikují cache invalidaci.

## HTTP Metody

Základní role metod:

- `POST` vytváří resource nebo reprezentuje command-like operaci,
- `PATCH` provádí částečnou aktualizaci,
- `PUT` nahrazuje celou reprezentaci,
- `GET` čte resource nebo kolekci.

Vlastnosti metod:

| Metoda | Typická operace | Body | Safe | Idempotentní | Cache | Typický status |
| --- | --- | --- | --- | --- | --- | --- |
| `POST` | vytvoření, výjimečně čtení | ano | ne | ne | může být | `201 Created` |
| `PATCH` | částečný update | ano | ne | ne | ne | `200 OK` |
| `PUT` | replace, výjimečně vytvoření | ano | ne | ano | ne | `200 OK`, případně `201 Created` |

`PUT` má posílat kompletní konzistentní strukturu. Pokud klient mění jednu vlastnost, vhodnější je `PATCH`.

Při vytvoření resource pomocí `POST` je vhodné:

- vrátit `201 Created`,
- vrátit vytvořený resource,
- přidat hlavičku `Location` s URL pro následný `GET`.

### Okrajové případy

`POST` pro čtení může dávat smysl, pokud:

- filtr je příliš složitý pro query string,
- v query stringu by se přenášely citlivé údaje.

Tím ale endpoint přestává být dostupný pro běžné vytváření resource přes `POST`, takže je nutné opatrně zvážit dopady.

`PUT` pro vytváření resource se známým ID je technicky možné, ale často se v praxi začne rozpadat, protože struktura pro vytvoření a aktualizaci se časem liší.

## Non-CRUD Interakce

Ne všechny operace jsou čistý CRUD. Místo sloves v URL je možné najít resource, který daná operace vytváří.

Nevhodné:

```text
invoices/{id}/pay
articles/{id}/publish
players/{id}/play
```

Vhodnější:

```text
payments
releases
player-events
account-activations
```

Příklady:

- zaplacení faktury vytvoří `payment`,
- publikování článku vytvoří `release`,
- kliknutí na play vytvoří `player-event`.
- aktivace účtu vytvoří `account-activation`.

Command payload pak nese data potřebná pro vytvoření daného pseudoresource, například:

```json
{
  "vs": 202656543,
  "total": 9999
}
```

```json
{
  "articleId": "k9hjs3a"
}
```

Tyto pseudoresources nemusí odpovídat databázové tabulce. Důležité je, že API má resource-oriented tvar a umožňuje přidat metadata operace.

Pokud už klient umí danou změnu udělat běžnou aktualizací resource, není nutné přidávat další endpoint jen pro stejnou věc.

## Content Negotiation

Různé reprezentace stejného resource se mají vyjednávat pomocí HTTP hlaviček:

```http
Accept: application/json
Accept: application/xml
Accept: application/pdf
Accept-Language: cs
```

URL zůstává stejná:

```text
invoices/{id}
```

Nevhodné pro běžné API resources:

```text
invoices/{id}.xml
invoices/{id}/export
```

Přípony v URL dávají smysl hlavně pro statické assety nebo soubory, které se přímo odkazují z uživatelského rozhraní, například CSS, JavaScript, WebP obrázky nebo někdy PDF.

Pokud klient požádá o formát, který API neumí vrátit, má server odpovědět:

```text
406 Not Acceptable
```

Neměl by tiše vracet JSON, pokud klient výslovně žádal XML.

## HATEOAS

Hypermedia odkazy mají ve webu jasnou roli, ale v běžných REST API často nepřinášejí dost hodnoty. Klienti většinou programují proti dokumentaci, neprocházejí API interaktivně přes odkazy.

Pro většinu API stačí vracet identifikátory souvisejících zdrojů:

```json
{
  "id": "article-1",
  "authorId": "author-1"
}
```

URL pro autora si klient složí podle dokumentace. Argument, že URL se může změnit, není dostatečný, protože změna URL v API je sama o sobě breaking change.

`_links` nebo HAL-like struktury dávejte do kontraktu jen tehdy, pokud pro ně existuje konkrétní klientská potřeba.

## Konvence Kontraktů

Kontrakt má být čitelný, predikovatelný a konzistentní.

JSON kontrakty by měly používat `camelCase`, protože JSON vychází z JavaScriptového ekosystému a většina klientů s tímto stylem přirozeně počítá.

### Standardizované hodnoty

Pro kódované hodnoty používejte existující standardy, pokud existují. Například pro jazyky je vhodné ISO 639-1:

```text
cs
sk
en
```

Vlastní hodnoty jako `CZ` místo `cs` se těžko mění, i při nové verzi API, protože klienti je už používají.

### Smysluplné názvy

Dobré názvy napovídají typ a význam:

```text
isActive
createdAt
documentType
```

Nekonzistentní varianty pro stejný koncept jsou zbytečně matoucí:

```text
startDate
dateFrom
dateStart
```

Vyberte jednu konvenci a používejte ji všude.

### Objekty místo dlouhých prefixů

Nevhodné:

```json
{
  "clientName": "...",
  "clientStreet": "...",
  "clientZip": "...",
  "supplierName": "...",
  "supplierStreet": "...",
  "supplierZip": "..."
}
```

Vhodnější:

```json
{
  "client": {
    "name": "...",
    "street": "...",
    "zip": "..."
  },
  "supplier": {
    "name": "...",
    "street": "...",
    "zip": "..."
  }
}
```

Související data patří do objektu, ne do dlouhých opakovaných prefixů.

## Chybové Struktury

API by mělo mít jednotnou chybovou strukturu pro všechny `4xx` a `5xx` odpovědi.

Dobrá výchozí volba je RFC 9457 Problem Details, v .NET reprezentované typem `ProblemDetails`, s content typem:

```http
Content-Type: application/problem+json
```

Základ může obsahovat:

```json
{
  "type": "https://restapi.cz/probs/problem-details",
  "title": "...",
  "detail": "...",
  "status": 400,
  "errors": [],
  "traceId": "..."
}
```

Validační chyby mohou přidat `errors`, ale základní tvar má zůstat stejný:

```json
{
  "title": "Validation failed",
  "detail": "One or more validation errors occurred.",
  "status": 400,
  "traceId": "...",
  "errors": {
    "name": ["Name is required."]
  }
}
```

Není vhodné vracet pro každý status code nebo endpoint jinou chybovou strukturu. Klient by pak musel implementovat mnoho speciálních větví.

## Priorita Chyb

Někdy jeden request obsahuje více chyb najednou:

- resource neexistuje,
- uživatel nemá oprávnění,
- body je nevalidní,
- požadovaný formát není podporovaný.

API má mít definovaná pravidla, která chyba vyhraje.

`Accept` a content negotiation by měly být vyhodnoceny velmi brzy, protože nejdříve je nutné určit, zda se klient a server vůbec umí domluvit na reprezentaci. Pokud požadovaný formát nelze vrátit, odpověď má být `406 Not Acceptable`.

Ostatní priority závisí na konkrétním API, routování a bezpečnostním modelu, ale měly by být konzistentní napříč endpointy.

## Verzování

Nejdůležitější otázka není, jak API verzovat, ale jak verzování oddálit nebo se mu vyhnout.

Verzování znamená:

- udržovat více variant endpointů,
- řešit sdílenou logiku mezi verzemi,
- zvyšovat riziko chyb,
- zvyšovat náklady na podporu klientů.

Základní URL s verzí je praktické řešení:

```text
/v1/resources
```

Hlavní práce ale spočívá v návrhu, který minimalizuje nutnost zavést `/v2`.

### Jak omezit potřebu verzování

- Navrhovat API jako celek, ne endpoint po endpointu.
- U každého kontraktu přemýšlet, jaké změny mohou přijít později.
- Preferovat zpětně kompatibilní změny.
- Raději zvolit trochu flexibilnější návrh než rychlé řešení, které bude brzy breaking change.
- U kontrolovaných klientů, například mobilních nebo desktopových aplikací, zvážit vynucenou aktualizaci klienta.
- Breaking changes komunikovat dopředu, pokud jsou nevyhnutelné.

U mobilních a desktopových aplikací může pomoci endpoint pro health check nebo minimální podporovanou verzi klienta. Klient pak při startu zjistí, že je příliš starý, a zobrazí požadavek na aktualizaci místo toho, aby server dlouhodobě držel staré API.

Někdy je lepší méně elegantní design než zavedení nové verze API jen kvůli malé změně.

## Logování a Traceability

Logování není jen pro řešení chyb. Je také zdrojem telemetry pro budoucí rozhodování o redesignu API.

Užitečné identifikátory:

- `requestId` jako unikátní serverový identifikátor konkrétního HTTP requestu v konkrétní službě,
- `traceId` pro sledování průchodu requestu přes více služeb,
- `correlationId` pro spojení více souvisejících requestů v jednom klientském scénáři.

`requestId` má generovat server. `correlationId` může dodat klient nebo API gateway. `traceId` obvykle vzniká pro jednu trasu requestu přes infrastrukturu, zatímco `correlationId` spojuje více samostatných requestů, které spolu patří z pohledu uživatelského nebo integračního scénáře.

Při chybě musí být možné rychle propojit klientem hlášený problém se serverovým logem.

Telemetry také pomáhá pochopit skutečné používání API. Například pokud se po každém načtení produktu volá deset dotazů na kategorie, může to ukazovat na problém v granularitě nebo reprezentaci endpointů.

Při návrhu nové verze API jsou provozní data cennější než odhady.

## Závěr

REST API by mělo být navržené jako dlouhodobý kontrakt. Konzistence, specifikace, validace, stabilní identifikátory, jednotné reprezentace, promyšlené chyby a dobrá telemetry pomáhají snížit počet breaking changes a zjednodušují integraci klientům.

Nejlepší čas odhalit chyby v API designu je před implementací: ve specifikaci, dokumentaci a mock serveru.
