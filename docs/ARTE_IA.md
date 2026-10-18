# Arte pintado con Nano Banana Pro

El juego trae arte generado por código, pero **cualquier imagen que pongas en `Assets/Umbral/Resources/ArtIA/` lo sustituye automáticamente**. No hace falta tocar código. Puedes ir poco a poco: lo que falte se sigue viendo con el arte procedural.

## Cómo hacerlo

1. Abre Gemini y elige el modelo de imágenes **Nano Banana Pro**.
2. Copia el bloque **ESTILO** de abajo y añade detrás el prompt de la imagen que quieras.
3. Descarga la imagen (PNG o JPG) y **renómbrala exactamente** como dice la tabla (por ejemplo `umr.png`).
4. Guárdala en su carpeta: `Assets/Umbral/Resources/ArtIA/<carpeta>/`.
5. Vuelve a Unity: la importa sola, quita el fondo magenta y la usa al pulsar Play.
   Si cambias una imagen y no se actualiza, usa el menú **Umbral → Reimportar arte IA**.

### Consejos para que todo parezca del mismo juego

- **Empieza cada prompt con el mismo bloque ESTILO.**
- Genera primero el **retrato** de un personaje. Después, al pedir su **figura de cuerpo entero**, adjunta el retrato como imagen de referencia y escribe «same character as the reference image». Nano Banana Pro mantiene así el diseño.
- Para que el fondo se pueda recortar: **fondo magenta liso** (#FF00FF), sin degradados ni sombras en el suelo. Si un personaje tiene mucho magenta (por ejemplo los de Azathoth), cambia a verde puro (#00FF00): el importador detecta cualquiera de los dos.
- Si una imagen sale con texto o marca de agua, pide «no text, no watermark» y repite.

### Orden recomendado (más impacto con menos imágenes)

1. Retratos de Sothis, los 6 dioses y 'Umr at-Tawil → diálogos y elección de boones.
2. Iconos de dioses → cartas de boon y HUD.
3. Fondos de menú y regiones → pantalla de título y entrada a cada región.
4. Suelos → todas las salas.
5. Figuras de cuerpo entero: primero Sothis, luego los enemigos del Bosque y Pickman.

---

## ESTILO (pégalo al principio de cada prompt)

```
Stylized hand-painted 2D video game illustration, bold thick black ink outlines, flat saturated colors with painterly cel shading, strong colored rim light, dramatic chiaroscuro, angular graphic shapes, subtle Art Nouveau ornament, dark gothic cosmic horror inspired by H. P. Lovecraft, clean readable silhouette, high detail, no text, no watermark.
```

### Formato por carpeta (añádelo después del prompt)

| Carpeta | Formato | Añade al prompt |
|---|---|---|
| `retratos/` | Vertical 2:3 | `Half-body character portrait, cut off at the waist at the bottom edge, solid flat pure magenta (#FF00FF) background, no background shadow.` |
| `personajes/` | Vertical 3:4 (1:1 para criaturas anchas) | `Full body, three-quarter view seen from slightly above, facing right, whole body visible with margin, feet near the bottom, empty hands, solid flat pure magenta (#FF00FF) background, no ground, no cast shadow.` |
| `iconos/` | Cuadrado 1:1 | `Single centered emblem icon, symmetrical, solid flat pure magenta (#FF00FF) background.` |
| `fondos/` | Horizontal 16:9 | `Wide cinematic environment illustration, no characters in the foreground.` |
| `suelos/` | Cuadrado 1:1 | `Seamless tileable top-down floor texture, orthographic, no perspective, no objects, even lighting, medium-dark values.` |

En los retratos, los allegados y los dioses salen a la izquierda del diálogo: pídelos **girados hacia la derecha** (`turned three-quarters to the right`). Sothis sale a la derecha: pídelo **girado hacia la izquierda** (`turned three-quarters to the left`).

---

## retratos/

| Archivo | Prompt |
|---|---|
| `sothis_f.png` | Young woman, daughter of the cosmic god Yog-Sothoth: pale lavender skin, long flowing silver-white hair, two small dark curled goat horns, glowing cyan eyes, a small glowing magenta third eye on her forehead, high-collared deep violet robe with tendril-like hems, three small iridescent glowing orbs floating around her, determined melancholic expression. |
| `sothis_m.png` | Young man, son of the cosmic god Yog-Sothoth: pale lavender skin, short spiky silver-white hair, two small dark curled goat horns, glowing cyan eyes, a small glowing magenta third eye on his forehead, high-collared deep violet robe with a hood on his shoulders and tendril-like hems, three small iridescent glowing orbs floating around him, determined melancholic expression. |
| `umr.png` | 'Umr at-Tawil, the veiled Guide of the Ultimate Gate: towering figure wrapped in layered violet and indigo veils, the face is only a soft oval of shifting pearlescent light, several iridescent spheres orbiting, serene and ancient. |
| `carter.png` | Randolph Carter, 1920s Boston gentleman dreamer: grey three-piece suit, red tie, neat grey hair and moustache, kind tired eyes, holding an ornate silver key, faint dreamlike blue glow. |
| `keziah.png` | Keziah Mason, 17th century Salem witch: wrinkled greenish skin, hooked nose, wild grey hair, tall crooked black witch hat, glowing green eyes, sly smile, crooked staff with a green orb. |
| `jenkin.png` | Brown Jenkin, the witch's familiar: a large brown rat with a disturbingly small human face, yellow eyes, long sharp teeth, tiny human-like hands, mischievous grin. |
| `zadok.png` | Zadok Allen, 96-year-old drunk sailor from Innsmouth: big white beard, red nose, old fisherman's cap and navy coat, clutching a whisky bottle, wide haunted eyes. |
| `west.png` | Herbert West, the Reanimator: slim young doctor, blond hair, round glasses, white lab coat stained, holding a syringe of glowing green reagent, cold obsessed expression. |
| `wilbur.png` | Wilbur Whateley: very tall goatish young man, elongated jaw, small horns, yellowish skin, old brown farmer's coat that barely hides tentacles beneath, resentful eyes. |
| `cthulhu.png` | Cthulhu, the Great Dreamer: colossal octopus-headed humanoid god, green-teal scaly skin, face tentacles, narrow bat-like wings, glowing red eyes, sunken city of R'lyeh behind, sea mist. |
| `hastur.png` | Hastur, the King in Yellow: tall figure in tattered flowing yellow robes, pallid expressionless mask, glowing Yellow Sign symbol, Carcosa twin suns behind. |
| `shub.png` | Shub-Niggurath, the Black Goat of the Woods: huge dark cloud-like mass of black flesh with many goat legs, mouths and tentacles, crown of curved horns, glowing green eyes, dark forest. |
| `nodens.png` | Nodens, Lord of the Great Abyss: ancient bearded god with flowing silver hair and beard, silver-violet robes, holding a trident crackling with lightning, starry abyss behind. |
| `ithaqua.png` | Ithaqua, the Wind-Walker: gigantic gaunt humanoid silhouette of snow and wind, burning red eyes, icy blue skin, blizzard around it. |
| `cthugha.png` | Cthugha, the Living Flame: a sun-like sphere of living fire with flame tendrils and a hint of a face, orange and white heat, fire vampires as sparks. |
| `azathoth.png` | Azathoth, the Blind Idiot God: a bubbling chaotic mass of magenta and black spheres and light at the center of the universe, faceless, cosmic swirl, amorphous pipers in the dark. |
| `gatos.png` | The cats of Ulthar: an elegant black cat with glowing green eyes sitting proudly, ghostly cats behind, moonlit rooftops of a dream town. |
| `yog.png` | Yog-Sothoth, the Key and the Gate: a vast conglomeration of iridescent glowing spheres in the void, prismatic, no face, overwhelming. |
| `boss_pickman.png` | Richard Upton Pickman as a ghoul painter: canine grey-green ghoul face, red beret, paint-stained artist smock, holding a palette and brush, yellow eyes, sly grin. |
| `boss_dagon.png` | Father Dagon: colossal fish-frog god, teal scales, bulging pale eyes, wide mouth, crown of red coral, giant trident, deep sea. |
| `boss_hydra.png` | Mother Hydra: majestic fish-woman queen with a serpentine tail, pale green scales, flowing fin-like crest, glowing pearl, melancholic. |
| `boss_sacerdote.png` | The High Priest Not To Be Described: robed figure wrapped in yellow silk, wearing a featureless yellow silk mask, golden glowing hands, windowless monastery. |
| `boss_nyarlathotep.png` | Nyarlathotep as the Black Pharaoh: tall pharaoh with pitch-black skin, gold and blue striped nemes headdress, burning orange eyes, ankh staff, cruel amused smile. |

## personajes/

Figuras de cuerpo entero (usa el formato de la tabla). Si ya tienes el retrato, adjúntalo como referencia.

| Archivo | Prompt |
|---|---|
| `sothis_f.png` / `sothis_m.png` | Same description as the portrait, full body, long violet robe touching the ground with small tentacle tendrils at the hem. |
| `umr.png`, `carter.png`, `keziah.png`, `jenkin.png`, `zadok.png`, `west.png`, `wilbur.png` | Same description as each portrait, full body standing pose. |
| `zoog.png` | Zoog: small furry brown creature like a weird rabbit-squirrel with a short cluster of pink tentacles instead of a snout, red glowing eyes. |
| `ghoul.png` | Ghoul: hunched grey-green humanoid with a canine face, long arms and sharp claws, crouching to attack. |
| `nightgaunt.png` | Night-gaunt: thin faceless black rubbery humanoid with inward-curving horns, membranous bat wings and a barbed tail, flying. |
| `gug.png` | Gug: giant furry monster with a vertical mouth splitting its head, four forearms, pink eyes on the sides of its head. |
| `deepone.png` | Deep One: fish-frog humanoid with grey-green scales, bulging eyes, gills, dorsal fin, holding a trident. |
| `deeppriest.png` | Deep One priest: fish-frog humanoid in a purple robe and golden tiara, staff with a glowing teal orb. |
| `polyp.png` | Flying Polyp: semi-invisible dark blue amorphous mass with many pale glowing eyes and tentacles, hovering, wind swirls. |
| `starspawn.png` | Star-spawn of Cthulhu: bulky green octopoid creature with face tentacles and small wings. |
| `lengman.png` | Man of Leng: satyr-like humanoid with a too-wide mouth, red robes, turban hiding small horns, goat legs, long spear. |
| `moonbeast.png` | Moon-beast: big pale greyish-white toad-like creature with no eyes and a cluster of pink tentacles for a snout. |
| `lengspider.png` | Spider of Leng: huge purple-grey spider with many glowing violet eyes. |
| `shantak.png` | Shantak: huge scaly bird with a horse-like head and bat wings, flying. |
| `migo.png` | Mi-Go: pinkish fungoid crustacean with membranous wings and a convoluted glowing head, many legs, flying. |
| `tindalos.png` | Hound of Tindalos: lean angular blue-black beast made of sharp angles, glowing blue eyes, long hollow tongue. |
| `piper.png` | Amorphous Piper: lumpy dark purple shapeless being playing a glowing golden flute. |
| `shoggoth.png` | Shoggoth: huge black iridescent protoplasmic blob covered in glowing green eyes and forming mouths. |
| `shoggoth_blob.png` | Small piece of shoggoth: little black blob with a few glowing green eyes. |
| `retono.png` | Dark Young of Shub-Niggurath: black tree-like creature with goat hooves, ropy tentacles and glowing green mouths. |
| `boss_pickman.png` | Same as the Pickman portrait, full body ghoul painter. |
| `boss_dagon.png`, `boss_hydra.png`, `boss_sacerdote.png`, `boss_nyarlathotep.png` | Same as each portrait, full body, imposing boss pose. |
| `nyar_morador.png` | Nyarlathotep as the Haunter of the Dark: black winged bat-like shape with a single three-lobed burning red eye, huge wings spread. |
| `nyar_caos.png` | Nyarlathotep as the Crawling Chaos: towering cone-shaped mass of black flesh with many tentacles, a faceless blood-red glowing cone instead of a face. |
| `fam_gato.png` | Ébano: sleek black cat with glowing green eyes, walking. |
| `fam_zoog.png` | Pipo: small friendly light-brown zoog with golden eyes and tiny pink snout tentacles. |
| `dummy.png` | Training dummy: wooden scarecrow with a ragged coat and a stuffed fish head. |

## iconos/

| Archivo | Prompt |
|---|---|
| `cthulhu.png` | Emblem of Cthulhu: teal octopus head with tentacles inside a circular frame. |
| `hastur.png` | The Yellow Sign glowing gold. |
| `shub.png` | Black goat head with curved horns and green glow. |
| `nodens.png` | Silver trident with violet lightning. |
| `ithaqua.png` | Icy blue snowflake with two red eyes. |
| `cthugha.png` | Living sun of orange flame. |
| `gatos.png` | Black cat head with green eyes. |
| `azathoth.png` | Chaotic magenta spiral eye. |
| `baculo.png` | Ebony staff topped with an ornate silver key. |
| `dagas.png` | Two crossed angular teal daggers. |
| `tomo.png` | Ancient leather grimoire with an eye on the cover and green flames. |
| `hoz.png` | Great golden-bladed war scythe. |
| `trapezoedro.png` | Black faceted crystal with red veins glowing. |

## fondos/

| Archivo | Prompt |
|---|---|
| `menu.png` | A lone robed figure with silver hair standing on a small floating rock island in an endless cosmic void, giant iridescent spheres floating in the sky, purple and teal nebula. Leave the left third darker and emptier (the title goes there). |
| `bosque.png` | The Enchanted Wood of the Dreamlands: gnarled ancient oaks, phosphorescent green fungi, fog, small eyes of zoogs in the dark. |
| `rlyeh.png` | Sunken R'lyeh: impossible non-Euclidean green stone architecture, tilted monoliths, deep teal water light, a colossal sleeping silhouette. |
| `leng.png` | The Plateau of Leng: frozen ochre plateau, windowless stone monastery, braziers, jagged mountains, golden dusk. |
| `azathoth.png` | The Court of Azathoth at the center of chaos: magenta cosmic maelstrom, amorphous pipers dancing, writhing tentacles, a blinding chaotic sun. |
| `muerte.png` | A figure dissolving into iridescent spheres while falling into a dark violet void. |
| `victoria.png` | Rolling green hills of Dunwich at sunset with ancient standing stones, a robed silver-haired figure seen from behind looking at the sky. |

## suelos/

| Archivo | Prompt |
|---|---|
| `umbral.png` | Violet and indigo stone tiles with faint glowing iridescent rune lines. |
| `bosque.png` | Mossy green forest stone path with roots and small glowing fungi. |
| `rlyeh.png` | Wet slimy green-teal cyclopean stone blocks with strange carvings. |
| `leng.png` | Cracked ochre frozen stone slabs with frost. |
| `azathoth.png` | Dark magenta organic floor of veined flesh and black crystal. |
