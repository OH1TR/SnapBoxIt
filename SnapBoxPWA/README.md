# SnapBox PWA

SnapBox on progressiivinen web-sovellus (PWA) komponenttien hallintaan. Sovellus on portattu SnapBoxApp MAUI-sovelluksesta Vue.js:llä ja **TypeScriptillä**.

## Ominaisuudet

- **Lataa kuva**: Ota kuva ja lataa se järjestelmään
- **Hae**: Etsi komponentteja järjestelmästä semanttisella haulla
- **Laatikot**: Selaa laatikoiden sisältöä
- **Tulosta**: Tulosta tarra tai QR-koodi
- **Asetukset**: Määritä API-asetukset

## Teknologiat

- **Vue 3** (Composition API + TypeScript)
- **TypeScript** - Tyyppiturvallisuus
- **Vue Router** - Sivujen välinen navigointi
- **Pinia** - Tilanhallintatyökalu
- **Axios** - HTTP-asiakaskirjasto
- **Vite** - Build-työkalu
- **Vite PWA Plugin** - PWA-tuki

> **Huom**: Projekti käyttää TypeScriptiä. Katso [TYPESCRIPT_MIGRATION.md](TYPESCRIPT_MIGRATION.md) lisätietoja TypeScript-muunnoksesta.

## Käyttöönotto

### Kehitysympäristö

1. Asenna riippuvuudet:
   ```bash
   npm install
   ```

2. Käynnistä kehityspalvelin:
   ```bash
   npm run dev
   ```

   Sovellus käynnistyy osoitteessa http://localhost:5173

3. TypeScript-tyyppitarkistus (valinnainen):
   ```bash
   npx vue-tsc --noEmit
   ```

### Kameran käyttö kehitysympäristössä

**TÄRKEÄÄ**: Kameran käyttö vaatii HTTPS-yhteyden tai localhost-osoitteen. Kehitysympäristössä on kaksi vaihtoehtoa:

#### Vaihtoehto 1: Käytä HTTPS:ää (suositeltu)

1. Luo kehitysvarmenne (kertaluonteinen toimenpide):
   ```bash
   # Windows PowerShell (Järjestelmänvalvojan oikeuksilla)
   dotnet dev-certs https --trust
   ```

2. Päivitä `vite.config.js` ottamaan HTTPS käyttöön:
   ```javascript
   server: {
     port: 5173,
     strictPort: true,
     https: true,  // Lisää tämä rivi
     proxy: {
       // ...existing proxy config
     }
   }
   ```

3. Käynnistä sovellus normaalisti:
   ```bash
   npm run dev
   ```

4. Avaa selaimessa: `https://localhost:5173`

#### Vaihtoehto 2: Käytä http://localhost (rajoitetusti)

Useimmat selaimet sallivat kameran käytön http://localhost -osoitteessa, mutta tämä ei toimi kaikissa selaimissa eikä mobiililaitteilla.

### Mobiilitestaus kehitysympäristössä

Jos haluat testata kameraa mobiililaitteella:

1. Käytä HTTPS:ää (Vaihtoehto 1 yllä)
2. Selvitä koneesi IP-osoite:
   ```bash
   ipconfig  # Windows
   ifconfig  # macOS/Linux
   ```
3. Päivitä `vite.config.js`:
   ```javascript
   server: {
     host: '0.0.0.0',  // Kuuntele kaikista verkkokäyttöliittymistä
     port: 5173,
     strictPort: true,
     https: true,
     // ...rest of config
   }
   ```
4. Avaa mobiililaitteella: `https://<koneesi-ip>:5173`
5. Hyväksy selaimen varoitus itse-allekirjoitetusta varmenteesta

### Tuotantoversio

1. Rakenna sovellus (sisältää TypeScript-tarkistuksen):
   ```bash
   npm run build
   ```

2. Tiedostot generoidaan `wwwroot` -kansioon, jota ASP.NET Core -sovellus voi käyttää.

3. **HUOM**: Tuotantopalvelimen täytyy käyttää HTTPS:ää kameran käytön mahdollistamiseksi.

## Asetukset

Ennen sovelluksen käyttöä, siirry Asetukset-sivulle ja määritä:

- **API URL**: SnapBoxApi:n URL-osoite (esim. https://api.snapbox.com)
- **Käyttäjätunnus**: API:n käyttäjätunnus
- **Salasana**: API:n salasana

Asetukset tallennetaan selaimen localStorageen.

## Kameran käyttö ja ongelmanratkaisu

### Virheviesti: "The object can not be found here"

Tämä virhe ilmenee, kun:
1. Sovellus ei ole HTTPS-yhteydessä (eikä localhost)
2. Selaimen kameratuki puuttuu
3. Käyttöoikeudet kameraan on estetty

**Ratkaisut:**
- Varmista että käytät `https://` -osoitetta tai `http://localhost`
- Tarkista selaimen asetuksista että kameran käyttö on sallittu
- Kokeile toista selainta (Chrome, Edge, Safari, Firefox tukevat kaikki kameraa)
- Mobiililaitteilla: hyväksy kameran käyttöoikeus kun selain kysyy

### Virhe: "NotAllowedError"
Käyttäjä on estänyt kameran käyttöoikeuden. Salli kamera selaimen asetuksista.

### Virhe: "NotFoundError"
Laitteessa ei ole kameraa tai sitä ei tunnisteta.

### Virhe: "NotReadableError"
Kamera on jo käytössä toisessa sovelluksessa. Sulje muut sovellukset jotka käyttävät kameraa.

## API-integraatio

Sovellus käyttää SnapBoxApi:a seuraaviin toimintoihin:

- `PUT /Image/upload/{boxId}` - Kuvan lataus
- `POST /Data/FindItems` - Komponenttien haku
- `PUT /Data/Save/{id}` - Komponentin tallennus
- `DELETE /Data/{id}` - Komponentin poisto
- `GET /Data/Image/{blobId}` - Kuvan haku
- `POST /Data/PrintLabel` - Tarran tulostus
- `GET /Data/GetBoxes` - Laatikoiden listaus
- `GET /Data/GetBoxContents/{boxId}` - Laatikon sisällön haku

## Projektinrakenne

```
ClientApp/
??? public/          # Staattiset tiedostot
??? src/
?   ??? components/  # Uudelleenkäytettävät komponentit
?   ??? views/       # Sivukomponentit
?   ??? stores/      # Pinia stores (TypeScript)
?   ??? services/    # API-palvelut (TypeScript)
?   ??? router/      # Vue Router -konfiguraatio (TypeScript)
?   ??? types/       # TypeScript-tyyppimäärittelyt
?   ??? App.vue      # Pääkomponentti
?   ??? main.ts      # Sovelluksen aloituspiste (TypeScript)
?   ??? env.d.ts     # TypeScript-ympäristömäärittelyt
?   ??? style.css    # Globaalit tyylit
??? index.html       # HTML-pohja
??? tsconfig.json    # TypeScript-konfiguraatio
```

## PWA-ominaisuudet

Sovellus tukee PWA-ominaisuuksia:

- Offline-tuki service workerin avulla
- Asennettavissa laitteelle
- Responsiivinen suunnittelu
- API-vastausten välimuistitus

## Lisenssit

Katso projektin juuressa oleva LICENSE-tiedosto.
