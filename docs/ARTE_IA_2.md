# Arte IA — segunda tanda: animación, efectos y escenografía (estilo Hades II)

Esta tanda acerca el juego a lo que se ve en el vídeo de Hades II:
- **Hojas de poses:** varios fotogramas por personaje, para que el movimiento sea más fluido.
- **Efectos pintados:** tajos, impactos, manchas y el círculo mágico.
- **Escenografía:** siluetas oscuras en primer plano y detalles pintados sobre el suelo.
- **Suelos sin cuadrícula:** terreno orgánico en lugar de losas.

Todo es opcional: lo que no exista se sigue viendo con los efectos y animaciones por código.

## Cómo entregarlas

- **Lo mejor:** que cada imagen ya lleve su **nombre exacto** (por ejemplo `zoog.png`) y vaya en su carpeta: `Assets/Umbral/Resources/ArtIA/animaciones/`, `efectos/`, `decorado/` o `suelos/`.
- **Si no te da tiempo a renombrarlas:** mándamelas en un zip como la vez anterior y yo las identifico.
- Usa el mismo bloque **ESTILO** de `docs/ARTE_IA.md` al principio de cada prompt.
- Fondo **magenta liso (#FF00FF)** en todo salvo en los suelos.

---

## 1. Hojas de poses → `animaciones/<id>.png`

Son 6 poses del mismo personaje en una rejilla de **3 columnas × 2 filas** (formato **3:2**, por ejemplo 1536×1024). El juego corta la rejilla y alterna las poses:

| Arriba | | |
|---|---|---|
| 1. Reposo | 2. Carrera (pie izquierdo delante) | 3. Carrera (pie derecho delante) |
| **Abajo** | | |
| 4. Preparación del golpe | 5. Golpe a máxima extensión | 6. Recibir daño / embestida |

**Imprescindible:** adjunta como **imagen de referencia** la figura que ya hiciste (`personajes/<id>`) para que salga el mismo personaje.

### Prompt base (cambia lo que va entre corchetes)

```
Character pose sheet of the SAME character as the reference image: identical design, colors, outfit and proportions. Six full-body poses arranged in a 3 columns x 2 rows grid of equal cells. Every pose at the same scale, feet resting on the same baseline near the bottom of each cell, all facing right, three-quarter view seen from slightly above. Top row: 1) idle standing pose, 2) running stride with left leg forward, 3) running stride with right leg forward. Bottom row: 4) anticipation pose winding up an attack, [ATTACK WINDUP], 5) attack at full extension, [ATTACK STRIKE], 6) [HURT OR DASH POSE]. No pose may cross into another cell. Solid flat pure magenta (#FF00FF) background, no grid lines, no borders, no text, no shadows on the ground.
```

### Qué poner en cada personaje

| Archivo | [ATTACK WINDUP] | [ATTACK STRIKE] | [HURT OR DASH POSE] |
|---|---|---|---|
| `sothis_f.png` / `sothis_m.png` | casting gesture, both hands pulled back gathering violet magic | arms thrust forward releasing a burst of violet magic, robe flaring | dashing forward low, robe and hair streaming behind |
| `zoog.png` | crouched low ready to pounce | leaping bite with tentacle snout spread | knocked back, tumbling |
| `ghoul.png` | hunched, claws raised behind the head | lunging claw swipe, arms extended | recoiling in pain |
| `deepone.png` | trident pulled back | trident thrust forward | staggered backwards |
| `deeppriest.png` | staff raised, orb glowing | staff pointed forward casting water | staggered backwards |
| `lengman.png` | spear pulled back, crouched | charging spear thrust | staggered backwards |
| `tindalos.png` | crouched, tongue out, about to spring | lunging bite | recoiling |
| `boss_pickman.png` | claws raised, brush in mouth | savage claw swipe | recoiling |
| `boss_dagon.png` | trident raised high | trident slammed down | roaring, staggered |
| `boss_nyarlathotep.png` | ankh staff raised, sand swirling | staff thrust forward, dark energy | staggered |
| `wilbur.png` | tentacles under the coat gathering | tentacles lashing out of the coat | recoiling |

**Criaturas voladoras** (`nightgaunt`, `shantak`, `migo`, `nyar_morador`): cambia las poses 2 y 3 por `wings fully raised` y `wings fully lowered` (aleteo).

**Prioridad:** Sothis primero (es lo que más se ve). Luego zoog, necrófago y Pickman, que son la primera región.

---

## 2. Efectos → `efectos/`

| Archivo | Formato | Prompt |
|---|---|---|
| `tajo.png` | 2:1 horizontal | A single straight horizontal brush-stroke slash effect band filling the whole width of the image, thick solid black ink along the TOP edge, a thin white-hot line just below it, then a bright violet-to-cyan glowing core, fading into dry-brush streaks toward the BOTTOM edge, sharp tapered points at the left and right ends, painterly hand-drawn game VFX. |
| `impacto.png` | 1:1 | Hit impact burst effect: a white-hot four-pointed star flash with sharp thin radiating spikes, small black ink splatter flecks around it, centered, hand-drawn game VFX. |
| `salpicadura.png` | 1:1 | A black ink splatter stain seen from directly above, irregular splash with droplets around it, flat, no shading. |
| `circulo.png` | 1:1 | A glowing occult magic circle seen from directly above: an Elder Sign star in the center, a double ring of runes, thin luminous violet and cyan lines, lots of empty space between lines. |

El **tajo** debe ser una **banda recta** (el juego la curva sola). La tinta va **arriba** y la parte deshilachada **abajo**.

---

## 3. Escenografía → `decorado/`

### Siluetas de primer plano → `decorado/<region>_frente_1.png`, `_2`, `_3`

Formato vertical 3:4. Se colocan entre la cámara y el escenario, en el borde inferior de la pantalla, como las plantas y rocas oscuras que enmarcan Hades.

```
Dark foreground silhouette of [ELEMENT], almost black with a subtle [COLOR] rim light on the edges, growing up from the bottom edge of the image, side view, solid flat pure magenta (#FF00FF) background.
```

| Región | [ELEMENT] | [COLOR] |
|---|---|---|
| `bosque` | gnarled twisted branches and ferns with a few glowing mushrooms | green |
| `rlyeh` | tall swaying kelp and broken cyclopean stone pillars | teal |
| `leng` | jagged frozen rocks and a tattered banner on a pole | golden |
| `azathoth` | writhing black tentacles and dark crystal shards | magenta |
| `umbral` | broken ornate columns and drifting veils | violet |

### Detalles de suelo → `decorado/<region>_suelo_1.png` … `_4`

Formato 1:1. Son manchas pintadas que se reparten por el suelo para romper la repetición.

```
Top-down view, flat on the ground, a single [DETAIL] with soft irregular edges, orthographic, no perspective, solid flat pure magenta (#FF00FF) background.
```

| Región | Ideas para [DETAIL] (una por imagen) |
|---|---|
| `bosque` | patch of moss with tiny glowing mushrooms · cluster of exposed roots · pile of dark fallen leaves · ancient carved stone circle half covered by moss |
| `rlyeh` | puddle of dark water with ripples · slimy seaweed patch · scattered shells and coral bits · carved non-euclidean glyph slab |
| `leng` | patch of snow with footprints · scattered bones and a skull · cracked ochre slab with frost · ritual circle drawn in yellow chalk |
| `azathoth` | pulsing veins of dark flesh · cluster of tiny purple crystals · eye-like nodule surrounded by tendrils · burnt chaotic rune circle |
| `umbral` | worn rug with occult symbols · stack of old books and candle wax · scattered iridescent pebbles · cracked tile with glowing rune |

---

## 4. Suelos sin cuadrícula → `suelos/<region>.png` (sustituyen a los actuales)

Los suelos actuales del Umbral y de Leng tienen losas en cuadrícula muy visibles. Hades usa terreno orgánico.

```
Seamless tileable top-down ground texture, organic natural terrain with NO tiles, NO grid, NO repeating squares, [TERRAIN], hand-painted, soft variation of values, medium-dark so characters stand out, orthographic, no perspective, no objects.
```

| Archivo | [TERRAIN] |
|---|---|
| `bosque.png` | dark mossy forest soil with roots, patches of grass and scattered glowing specks |
| `rlyeh.png` | wet uneven green-teal stone with slime and shallow water stains |
| `leng.png` | cracked ochre earth with frost and thin snow drifts |
| `azathoth.png` | dark magenta organic ground of veined flesh and black glassy patches |
| `umbral.png` | worn dark violet flagstones of irregular shapes with faint glowing rune lines and moss in the cracks |
