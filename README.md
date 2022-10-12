# Inlämningsuppgift backend - En pizzeria
Inlämningsuppgiften går ut att implementera en simulering av en pizzeria. Det är
alltså *inte* en onlinepizza-klon utan applikationen simulerar bland annat
pizzabagare som tillagar pizzor. Applikationen innehåller koncept och tekniker
ni lärt er under kursen. Rättningen av uppgiften kommer göras med automatiska
tester. Det kommer alltså inte ske en manuell granskning av din kod utan så
länge testerna går grönt så är du godkänd.

Din kod kommer skrivas i projektet `BackendExam`. I projektet `ExamContext`
finns modellklasser och tjänster som ska konsumeras för att lösa uppgiften. I
projektet `BackendExam.Tests` finns de automatiska tester som testar att din
lösning är okej. Du ska som sagt skriva din kod i `BackendExam`-projektet men
det är okej att läsa koden i de andra projekten. Att läsa koden i `ExamContext`
är förmodligen nödvändigt för att förstå hur du ska lösa uppgiften.

Vi har valt en domän som inte är realistisk, du skulle aldrig implementera en
pizzeria-simulator med ett web api. Vår tanke att det ska vara en kul grej så
fastna inte i att försöka göra allt logiskt. Vi hoppas att ni kommer uppskatta
det och tycka att det är en rolig uppgift att koda. Tänk även på att vi i vissa
fall kan ha valt krav eller implementationsdetaljer som är extra omständliga att
jobba med. Vi har gjort detta för att det är en examination och vi vill se att
ni klarar av att hantera det.

## Översikt av uppgiften
Här följer en översikt av funktionerna i uppgiften. Mer information om varje del
finns senare i dokumentet.

* Applikationen ska kunna lista pizzerians pizzor (menyn)
* Applikationen ska kunna ta emot beställningar med en eller flera pizzor i
* Applikationen ska tillåta att kunderna hämtar ut beställningar som är färdiga
* Applikationen ska göra det möjligt för anställda att stämpla in och ut sig
  från pizzerian när de börjar och slutar för dagen
* Applikationen ska göra det möjligt för ägaren att lägga till nya ingredienser
  i lagret
* Anställda och ägaren måste logga in för göra sina operationer
* Det ska också vara möjligt att lägga till flera anställda i systemet
  (registrering)

### Endpoints
Nedan är en komplett lista av de endpoint som ska finnas i applikationen. För
varje endpoint finns en kort beskrivning av vad den är till för. Mer detaljerade
krav för applikationens funktioner finns beskrivna senare i dokumentet.

* Login
  * POST /login -- tar emot användarnamn och lösenord och returnerar en
    JWT-token i body
  * POST /register -- tillåter registrering av nya användare (medarbetare)
* Menu
  * GET /menu -- listar alla pizzor som går att beställa
* Order
  * POST /order -- lägger en beställning på en eller flera pizzor
  * GET /order/{order-id} -- kollar status på en order. Om ordern är klar
    returneras pizzorna i order:n
* Restaurant
  * POST /restaurant/enter -- en anställd anländer till restaurangen. Bara
    personer som är inloggade och har rollen `Employee` ska få anropa denna
    endpoint
  * POST /restaurant/leave -- en anställd lämnar restaurangen. Bara personer som
    är inloggade och har rollen `Employee` ska få anropa denna endpoint
  * POST /restaurant/add-ingredient -- en restaurangchef lägger till en
    ingrediens i lagret. Bara personer som är inloggade och har rollen `Manager`
    ska få anropa denna endpoint. Enbart ingrediens-typer som redan finns får
    läggas till.

## Meny
Applikationen ska returnera en lista med pizzerians pizzor (menyn). Användaren
  hämtar menyn med en GET-request mot `/menu`. Svaret på request:en ska vara en
  JSON och bestå av en lista med objekt. Varje objekt ska ha två fält: namnet på
  pizzan och priset på pizzan. Exempelsvar:

  ```json
  [
    { name: 'Vesuvio', price: 49.99 },
    { name: 'Favoriten', price: 69.99 },
    { name: 'Hawaii', price: 59.99 }
  ]
  ```

## Lägga beställningar
Det ska vara möjligt att lägga beställningar av pizzor. Beställningar läggs som
en POST-request mot `/order` där request-body ska vara en JSON-array med
pizza-namn. Bara namn som finns på menyn ska vara godkända. Om kunden vill
beställa två pizzor av samma sort ska det namnet skickas in två gånger. Mottagna
pizzor ska läggas på `OrderQueue`. Exempel på request:

```json
["Vesuvio", "Hawaii", "Favoriten", "Vesuvio"]
```

Returvärdet från din applikation ska ha
* Statuskod 201
* En HTTP header med namn Location och värdet `/order/{order-id}` där "order-id"
  är ID:t för den order som precis skapats
* Ingen response body

## Hämta ut beställning
Det ska vara möjligt att hämta en beställning av pizzor. Beställningar hämtas ut
som en GET-request mot `/order/[order-id]`. Där `order-id` är det ID vi får från
tjänsten när vi lägger en beställning. Om en order inte är klar ska
retur-statusen vara 404. Detsamma gäller om kunden försöker hämta en order som
inte finns. Om ordern är klar ska de pizzor som bakats returneras. Varje pizza
får ett unikt ID när den bakas i ugnen och det är viktigt att rätt IDn skickas
ut till användaren.

Om ordern är klar ska ordern returneras med ett 200-svar på följande format:

```json
{
  "status": "done",
  "order": [
    {
      "id": "[Pizza.Id]",
      "type": "[Pizza.Name]",
    }
  ]
}
```

När ordern har hämtats med `/order/{order-id}` ska den tas bort ifrån
`DeliveryDesk`. Om `/order/{order-id}` anropas igen efter det att ordern tagits
bort ifrån `DeliveryDesk` ska ett 404-svar skickas.

## Kock (Chef)
Du ska ta fram en kock genom att implementera `IChef`. `ExamContext` kommer
skapa upp flera instanser av den här klassen och köra dem i varsin tråd.
Applikationen förväntar sig att tråden är igång så länge applikationen körs.
Kockens uppgift är att hämta ordrar från OrderQueue, tillaga pizzorna i ordern
och leverera pizzorna till `DeliveryDesk`.

De tjänster kocken kommer interagera med (`OrderQueue`, `Warehouse`, `Oven` och
`DeliveryDesk`) är inte thread-safe. Det är upp till dig att göra anropen till
dem trådsäkra. `ExamContext` kommer starta varje kock i en tråd och sen anropa
`IChef.Run()`. Applikationen förväntar sig att tråden är igång så länge
applikationen körs. Så se till att din implementation inte returnerar ur `Run()`
även om kocken inte har något att göra för tillfället.

Översikt av kockens uppgift:
* Hämta order från `OrderQueue`
* I `Cookbook` finns information om vilka och hur många ingredienser varje pizza
  behöver
* Hämta ingredienserna från `Warehouse`
* Använd `Oven` för att baka pizzorna genom att tillhandahålla ingredienser.
  `Oven` kommer tillaga korrekt pizza baserat på ingredienserna som anges.
* Färdiga pizzor levereras till `DeliveryDesk`

Kocken ska initialt vänta på att en order kommer in i `OrderQueue`. När en order
kommer in så ska en av kockarna ta hela den ordern och börja förbereda pizzorna.
När hela ordern är tillagad går kocken tillbaka till att vänta på nya ordrar.

En order innehåller bara pizzanamn och antal pizzor som ska tillagas. Använd
`Cookbook` för att slå upp vilka ingredienser som behövs för varje typ av pizza.
Ingredienser hämtas från `Warehouse`. Bara en kock kan vara i lagret åt gången.

Om det inte finns tillräckligt med ingredienser för att tillaga en pizza ska
kocken göra så gott det går och tilllaga pizzan med de ingredienser som faktiskt
finns. Om ingredienserna matchar en annan pizza i kokboken kommer en sådan pizza
tillverkas. Annars skapar ugnen en speciell typ av pizza som kallas
`invalid-pizza`. Oavsett vilken typ av pizza som ugnen producerar ska du lägga
till den i ordern och skicka med den till `DeliveryDesk`. Ugnen kommer sköta
beslutet av vilken pizza som ska bakas men du måste se till att din lösning
hanterar att en ingrediens tar slut och att lösningen inte är beroende av vilken
typ av pizza som produceras.

## Login & registrering
Anställda och ägare ska kunna logga in i systemet för att komma åt funktionerna
med stämpelklockan och lägga till ingredienser.

En inloggning görs med en POST mot `/login` med följande request-body:
```json
{
  "Username": "[your-user-name]",
  "Password": "[your-password]"
}
```

Svaret på en login är ett 202-svar med följande response-body:
```json
{
  "Value": "[jwt-token]"
}
```

Det är också möjligt att registrera nya användare i systemet. En registrering
görs med en POST mot `/register` och med reques-body:
```json
{
  "Username": "[your-user-name]",
  "Password": "[your-password]",
  "Roles": [
    "Employee",
    "Manager"
  ]
}
```

Svaret på en registrering är ett 200-svar utan en body.

## Uppgifter för anställda
En anställd ska kunna registrera sig som ankommen till jobbet samt registrera
att hen lämnar restaurangen. Detta görs med `/restaurant/enter` respektive
`/restaurant/leave`. Dessa requests ska registrera i `TimeClock` att den
inloggade användaren är på jobbet eller inte. Bara användare med rollen
`Employee` får anropa denna endpoint. För personer som inte är inloggade eller
saknar rollen ska korrekt statuskod returneras utan en responsekropp. Ingen
requestkropp behöver skickas med. All data som ska användas läses från den
inloggade användaren. För att inte göra uppgiften för komplicerad är denna
funktion frikopplad från andra funktioner i systemet.

Restaurangchefen ska kunna lägga till nya ingredienser via
`/restaurant/add-ingredients`. Bara personer med rollen `Manager` ska få anropa
den här endpointen. För personer som inte är inloggade eller saknar rollen ska
korrekt statuskod returneras utan en responsekropp. Varje request skickar in en
(1) ingrediens. Ingrediensen ska läggas till i `Warehouse`.

## Tjänster (dependency injection)
I Program.cs finns ett anrop till `builder.Services.AddExamContext()`. Detta
anrop sätter upp de tjänster som behövs för att köra applikationen. Om ni skulle
råka ta bort detta anrop kommer inte applikationen att fungera.

Efter anropet till `AddExamContext` kommer följande tjänster att vara
tillgängliga. Vi registrerar även tjänster vi behöver internt i `ExamContext`.
Tjänsterna i listan nedan är tjänster som är relevanta för er kod. Vi inkluderar
en kort beskrivning av varje tjänst här men det är en del av inlämningen att
förstå hur tjänsterna fungerar.

**Cookbook**

Kocken använder kokboken för att veta vilka ingredienser hen ska använda för att
tillaga en pizza. Blanda inte ihop kokboken med menyn. På den här pizzerian
skriver de inte ut ingredienserna på menyn, bara namnet och priset. Kokboken är
den tjänst som kan översätta en pizzatyp till ingredienser.

**DeliveryDesk**

Representerar en bänk i pizzerian där färdiga pizzor placeras. Pizzorna
grupperas enligt den order de tillhör så att personalen lätt kan veta vilka
pizzor som hör till en viss order. Ibland kan kunderna vara ivriga och tjata om
att få sin order levererad. Se till att du inte råkar skicka tillbaka samma
order mer än en gång när det kommer in flera förfrågningar om orderns status.

**Menu**

Menyn kunderna använder för att veta vilka pizzor som går att beställa från
pizzerian. Menyn innehåller bara information om pizzornas namn och pris. Det
finns ingen information om ingredienser. Tänk på att testerna som kommer
verifiera applikationen inte alltid håller menyn och kokboken i synk. Så skriv
ingen kod som förutsätter att de båda innehåller en viss typ av pizza.

**OrderQueue**

Representerar en plats i pizzerian där kassapersonalen lägger ordrar som kunder
beställt. Det är aldrig samma person som tar emot ordern som tillagar pizzorna.
Din lösning måste alltså ha en del som lägger till beställningar i kön och en
parallell del som kollar om det kommit in något nytt i kön. Ibland kan det
uppstå bråk vid orderkön då flera kockar försöker börja på samma order. Det är
därför viktigt att din lösning innehåller kod för att förhindra detta.

**Oven**

Representerar pizzerians ugn. Denna ugn är lite speciell. Given en lista med
ingredienser kan den alltid lista ut vilken pizza (från pizzerians kokbok) som
du försöker tillaga. Om listan med ingredienser inte matchar någon pizza i
kokboken kommer ugnen försöka tillaga den pizza som är närmast i ingredienser.
Den kan också tillaga oändligt många pizzor samtidigt, även om varje tillagning
tar en stund.

**TimeClock**

En stämpelklocka som pizzerians personal använder för att registrerar när de
kommer till jobbet och när de lämnar det. Stämpelklockan kan bara användas av
personer som är anställda på pizzerian. Det är fysiskt omöjligt för mer än en
person att använda stämpelklockan samtidigt. Se till att din lösning hanterar
detta.

**UserRepository** En "databas" som innehåller pizzerians anställda. Använd den
för att spara registrerade användare och kontrollera inloggade användare.

**Warehouse**

Det lager där pizzerian lagrar alla sina ingredienser. Varje ingrediens är unik
så om du vill ha två av någon typ av ingrediens måste du hämta ut två instanser
av den sorten. Att hämta en ingrediens tar en liten stund och du kan bara hämta
en ingrediens åt gången. Det är trångt i dörren till lagret så bara en kock kan
vara där inne i taget. Om en till kock försöker gå in kommer de krocka. Din
lösning måste förhindra att det händer.

### Registrera tjänster
För att applikationen ska fungera måste du registrera en singleton service av
typen `Storage.Chef.IChefFactory`. För att göra det måste du också skapa en
implementation av det interface:et.

Efter att applikationen skapas i Program.cs kommer `app.BootstrapExamContext()`
anropas. Den metoden kommer starta igång diverse funktioner som behövs inne i
`ExamContext`. Om du inte registrerat tjänsten korrekt kommer applikationen
skriva ut ett felmeddelande i console. Så håll uppsikt i console:en om du
upplever att din kock inte kör. Det kan vara så att du inte konfigurerar
tjänsterna korrekt.

## Parallellprogrammering
Både pizzerians kunder och dess personal kan ibland vara lite ivriga. Det är
därför viktigt att alltid se till att alla resurser som flera trådar kan försöka
nå samtidigt är skyddade på korrekt sätt. Det finns test som testar för att dett
krav följs.

## Betygsättning & Rättning
Rättningen kommer ske med automatiska tester som vi skrivit. Du kan själv köra
testerna i `BackendExam.Tests` för att se om din lösning kommer bli godkänd. För
att få G måste din lösning klara 70% av testerna (34 stycken) och för VG måste
din lösning klara 100% av testerna. Denna text är en hjälp för att förklara vad
uppgiften går ut på men det är testerna som är master. Så om något inte stämmer
överens mellan texten och testerna så är det testerna som stämmer. Om ni hittar
något som inte stämmer mellan texten och testerna får ni gärna höra av er.

Vissa tester överlappar med varandra. Detta kanske inte är optimalt i en riktigt
applikation men vår tanke här är att det ska underlätta för er om ni klarar av
att implementera en del av en funktion så kommer ni få några korrekta tester
(poäng).

Det är inte tillåtet att ändra koden i projekten `BackendExam.Tests` och
`ExamContext`. Om du ändrar i något av projekten och vi bedömer att det var
avsiktligt kommer vi klassa det som försök till fusk och din inlämning kommer
bli underkänd. Det är tillåtet att diskutera lösningar med andra och även att ta
inspiration från lösningar online. Det är *inte* tillåtet att kopiera någon
annans lösning (eller delar av en lösning) rakt av.

## Annat & kommentarer
Den uppmärksamme kan notera att vi inte använder pizzadegar eller tomatsås i
våra pizzor. Vi hade en tanke på att bygga en funktion kring detta men landade i
att den inte gav något till själva examineringen av uppgiften. Så i stället har
vi pizzor som inte har någon deg. Vi får använda vår fantasi och låtsas som att
det finns en deg och tomatsås.
