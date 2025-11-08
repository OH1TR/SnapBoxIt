# SnapBox Voice Commands - Complete Reference

## Kaikki ƒ‰nikomennot (Suomeksi)

### ?? Haku ja Navigointi

| Komento | Toiminto | Esimerkki |
|---------|----------|-----------|
| "Miss‰ on X?" | Etsi tavara ja n‰yt‰ tulokset | "Miss‰ on ruuvimeisseli?" |
| "Etsi X" | Etsi ja n‰yt‰ hakutulokset | "Etsi kaapeleita" |
| "Hae X" | Sama kuin etsi | "Hae tyˆkaluja" |
| "Lˆyd‰ X" | Etsi tietty tavara | "Lˆyd‰ vasara" |

**Tulokset:**
- Suorittaa haun
- Navigoi hakun‰kym‰‰n (/search)
- N‰ytt‰‰ tulokset automaattisesti
- Kertoo montako tulosta lˆytyi

---

### ?? Laatikkon‰kym‰

| Komento | Toiminto | Esimerkki |
|---------|----------|-----------|
| "N‰yt‰ laatikko X" | Avaa laatikon sis‰ltˆ | "N‰yt‰ laatikko BOX-001" |
| "Mit‰ laatikossa X on?" | Listaa laatikon tavarat | "Mit‰ laatikossa BOX-002 on?" |
| "Avaa laatikko X" | Sama kuin n‰yt‰ | "Avaa laatikko BOX-003" |

**Tulokset:**
- Navigoi laatikkon‰kym‰‰n (/boxes)
- Valitsee laatikon automaattisesti
- N‰ytt‰‰ kaikki tavarat laatikossa
- Kertoo montako tavaraa lˆytyi

---

### ?? Kuvaus ja Lis‰‰minen

| Komento | Toiminto | Vaatimus | Esimerkki |
|---------|----------|----------|-----------|
| "Lis‰‰ uusi" | Siirry lataussivulle | - | "Lis‰‰ uusi tavara" |
| "Uusi kohde" | Sama kuin lis‰‰ uusi | - | "Uusi kohde" |
| "Valitse laatikko X" | Aseta kohdelaat ikko | - | "Valitse laatikko BOX-001" |
| "Ota kuva" | Ota kuva kameralla | Laatikko valittuna | "Ota kuva" |

**Laatikko + Kuvaus workflow:**
```
1. "Lis‰‰ uusi" ? Siirtyy lataussivulle
2. "Valitse laatikko BOX-001" ? Asettaa laatikon
3. "Ota kuva" ? Ottaa kuvan ja lataa palvelimelle
```

---

### ?? Tietojen Hallinta (Tulossa)

| Komento | Toiminto | Tila |
|---------|----------|------|
| "P‰ivit‰ tavara X" | Muokkaa tietoja | ?? Tulossa |
| "Poista tavara X" | Poista kohde | ?? Tulossa |
| "Siirr‰ X laatikkoon Y" | Siirr‰ kohde | ?? Tulossa |

---

## Komento-esimerkit Skenaarioittain

### Skenaar io 1: Etsin tavaraa

```
?? K‰ytt‰j‰: "Miss‰ on ruuvimeisseli?"

?? AI: 
  1. Etsii "ruuvimeisseli" varastosta
  2. "Lˆysin 3 ruuvimeisseli‰ varastosta"
  3. Navigoi hakun‰kym‰‰n
  4. N‰ytt‰‰ tulokset kuvien kera

?? Tulos:
  - Ruuvimeisseli #1: BOX-001 (Phillips)
  - Ruuvimeisseli #2: BOX-001 (Flat)
  - Ruuvimeisseli #3: BOX-005 (Torx)
```

### Skenaar io 2: Tarkistan laatikon

```
?? K‰ytt‰j‰: "N‰yt‰ mit‰ laatikossa BOX-002 on"

?? AI:
  1. "Siirryn laatikkon‰kym‰‰n"
  2. Navigoi /boxes?box=BOX-002
  3. Lataa BOX-002 sis‰llˆn
  4. "Laatikossa on 7 kohdetta"

?? Tulos:
  - N‰ytt‰‰ kaikki 7 tavaraa
  - Kuvat ja kuvaukset
  - Kategoriat ja m‰‰r‰t
```

### Skenaar io 3: Lis‰‰n uuden tavaran

```
?? K‰ytt‰j‰: "Lis‰‰ uusi"

?? AI:
  1. "Siirryn lataussivulle"
  2. Navigoi /upload
  3. Kamera k‰ynnistyy automaattisesti

?? K‰ytt‰j‰: "Valitse laatikko BOX-003"

?? AI:
  1. "Laatikko BOX-003 valittu"
  2. T‰ytt‰‰ laatikko-kent‰n
  3. Tallentaa laatikon muistiin

?? K‰ytt‰j‰: "Ota kuva"

?? AI:
  1. "Otan kuvan laatikkoon BOX-003"
  2. Ottaa kuvan kameralla
  3. Lataa palvelimelle
  4. N‰ytt‰‰ esikatselun
  5. AI analysoi kuvan
  6. T‰ytt‰‰ otsikon, kategorian ja kuvauksen

?? Tulos:
  - Kuva otettu ?
  - Ladattu ?
  - Analysoitu ?
  - Valmis tallennettavaksi ?
```

### Skenaar io 4: Virhetilanne (ei laatikkoa)

```
?? K‰ytt‰j‰: "Ota kuva"

?? AI:
  ?? "Laatikko pit‰‰ valita ensin"
  ?? "Sano esimerkiksi 'valitse laatikko BOX-001'"

?? K‰ytt‰j‰: "Valitse laatikko BOX-004"

?? AI:
  ? "Laatikko BOX-004 valittu"
  ? "Nyt voit ottaa kuvan"

?? K‰ytt‰j‰: "Ota kuva"

?? AI:
  ?? Ottaa kuvan onnistuneesti
```

---

## AI:n ƒlykk‰‰t Ominaisuudet

### 1. Kontekstitietoisuus

AI ymm‰rt‰‰ kontekstin ja p‰‰telee tarkoituksen:

```
"Miss‰ vasara" ? Sama kuin "Miss‰ on vasara?" tai "Etsi vasara"
"BOX-001 sis‰ltˆ" ? Sama kuin "N‰yt‰ laatikko BOX-001"
"Uusi tavara" ? Sama kuin "Lis‰‰ uusi"
```

### 2. Virheenk‰sittely

AI antaa selke‰t ohjeet kun jotain puuttuu:

```
? Kuva ilman laatikkoa ? "Valitse laatikko ensin"
? Ep‰selv‰ komento ? "En ymm‰rt‰nyt, kokeile sanoa..."
? Laatikkoa ei lˆydy ? "Laatikkoa BOX-999 ei lˆydy"
```

### 3. Vahvistukset

AI vahvistaa toiminnot ennen suorittamista:

```
"Poista tavara X" ? "Haluatko varmasti poistaa?"
"Siirr‰ tavara" ? "Siirr‰n tavaran X laatikkoon Y, jatketaanko?"
```

---

## J‰rjestelm‰viestit Chat-Historiassa

K‰ytt‰j‰ n‰kee seuraavat ikonit ja viestit:

| Ikoni | Viesti | Merkitys |
|-------|--------|----------|
| ?? | "Siirryt‰‰n hakun‰kym‰‰n hakusanalla..." | Hakuun navigointi |
| ?? | "Siirryt‰‰n laatikkon‰kym‰‰n: BOX-X" | Laatikkoon navigointi |
| ?? | "Siirryt‰‰n lataussivulle" | Upload-sivulle |
| ? | "Laatikko BOX-X valittu" | Laatikko asetettu |
| ?? | "Otetaan kuva laatikkoon: BOX-X" | Kuva otetaan |
| ?? | "Laatikko pit‰‰ valita ensin!" | Varoitus |
| ?? | "Suoritetaan toiminto: search_items..." | Funktio suoritetaan |
| ?? | "Siirryt‰‰n sivulle: /X" | Geneerinen navigointi |

---

## Vinkkej‰ K‰yttˆˆn

### ? Hyv‰t K‰yt‰nnˆt

1. **Puhu selke‰sti** - Normaali keskustelunopeus toimii parhaiten
2. **K‰yt‰ suomea** - AI on optimoitu suomen kielelle
3. **Ole spesifinen** - "BOX-001" parempi kuin "eka laatikko"
4. **Valitse laatikko ensin** - Ennen kuvan ottamista
5. **Tarkista historia** - Chat n‰ytt‰‰ mit‰ tapahtui

### ? V‰lt‰

1. **Liian nopeaa puhetta** - AI ei ehdi tulkita
2. **Melua taustalla** - Voi h‰irit‰ ‰‰nitunnistusta
3. **Kuvaa ilman laatikkoa** - Tuloksena virhe
4. **Ep‰m‰‰r‰iset komennot** - Ole tarkka

---

## Pikan‰pp‰imet & Oikotiet

### Nopeat Komennot

```
"Hae" ? Siirtyy hakun‰kym‰‰n (tyhj‰ haku)
"Laatikot" ? Siirtyy laatikkon‰kym‰‰n
"Uusi" ? Siirtyy lataussivulle
"Takaisin" ? Edelliselle sivulle (tulossa)
```

### Yhdistetyt Komennot (Tulossa)

```
"Etsi vasara ja n‰yt‰" ? Haku + navigointi
"Uusi BOX-001 tyˆkalu" ? Laatikko + kategoria
"Ota kuva BOX-002" ? Laatikko + kuva
```

---

## Yhteenveto Toiminnoista

### ? Toteutettu (Nyt K‰ytˆss‰)

| Toiminto | Komennot | Tila |
|----------|----------|------|
| Haku ja navigointi | "Miss‰ on X", "Etsi X" | ? |
| Laatikkon‰kym‰ | "N‰yt‰ laatikko X" | ? |
| Lataussivu | "Lis‰‰ uusi" | ? |
| Laatikon valinta | "Valitse laatikko X" | ? |
| Kuvan ottaminen | "Ota kuva" | ? |
| Virheenk‰sittely | Automaattinen | ? |
| Suomenkielinen AI | Kaikki komennot | ? |

### ?? Tulossa (Tulevaisuus)

| Toiminto | Ehdotetut Komennot | Tila |
|----------|-------------------|------|
| P‰‰sivulle navigointi | "Mene p‰‰sivulle" | ?? |
| Asetukset | "Avaa asetukset" | ?? |
| Tulostus | "Tulosta tarra X" | ?? |
| Kohteen muokkaus | "P‰ivit‰ tavara X" | ?? |
| Kohteen poisto | "Poista tavara X" | ?? |
| Siirt‰minen | "Siirr‰ X laatikkoon Y" | ?? |
| Batch-kuvaus | "Ota 3 kuvaa" | ?? |
| ƒ‰nilomake | "Aseta m‰‰r‰ 5" | ?? |

---

## Tekniset Vaatimukset

### Selain

? Chrome 90+
? Edge 90+
? Safari 15+
? Firefox 88+

### Yhteys

? HTTPS (tai localhost)
? Vakaa internetyhteys (OpenAI API)
? Mikrofonin lupa
? Kameran lupa (kuvaus)

### Laite

? Mikrofoni
? Kamera (kuvaus)
? Moderni laite (2020+)

---

## Vianm‰‰ritys

### ƒ‰ni ei toimi

```
Ongelma: Mikrofonikuvake ei muutu vihre‰ksi
Ratkaisu:
  1. Tarkista mikrofonin lupa selaimessa
  2. Tarkista ett‰ HTTPS-yhteys
  3. Kokeile p‰ivitt‰‰ sivu
  4. Tarkista OpenAI API-avain
```

### Komento ei toimi

```
Ongelma: AI ei reagoi komentoon
Ratkaisu:
  1. Tarkista ett‰ yhteys on p‰‰ll‰ (vihre‰ mikrofoni)
  2. Puhu selke‰mmin
  3. Kokeile toisin sanoen
  4. Tarkista chat-historia mit‰ AI kuuli
```

### Kuvaa ei voi ottaa

```
Ongelma: "Laatikko pit‰‰ valita ensin"
Ratkaisu:
  1. Sano "Valitse laatikko BOX-XXX"
  2. Odota vahvistus
  3. Sano "Ota kuva"
```

---

## Tuki ja Palaute

### Logi ja Debuggaus

Chat-historia n‰ytt‰‰:
- Kaikki komennot (sininen)
- AI:n vastaukset (vihre‰)
- J‰rjestelm‰viestit (harmaa)
- Toiminnot (?? ikoni)

### Yhteystiedot

Ongelmat tai ehdotukset:
- GitHub Issues
- Projektin yll‰pit‰j‰

---

**Dokumentaatio:** v2.0
**P‰ivitetty:** 2024
**Kieli:** Suomi
**Tila:** ? Aktiivinen
