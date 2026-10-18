# Documento de diseño — UMBRAL

## 1. Análisis de Hades II y cómo se adapta

| Sistema de Hades II | Qué hace allí | En UMBRAL |
|---|---|---|
| Melinoë, princesa del Inframundo | Protagonista que intenta derrotar a Cronos | **Sothis Whateley**, hijo/hija de Yog-Sothoth, que intenta escapar del reino de los Dioses Exteriores |
| Encrucijada (hub) | Santuario entre incursiones | **El Umbral**, plaza flotante en el espacio entre los espacios |
| Hécate (mentora, primer jefe) | Guía y entrenadora | **'Umr at-Tawil**, el Guía (avatar de Yog-Sothoth) |
| Odiseo / Lista de profecías | Objetivos con recompensa | **Randolph Carter** / **Presagios** |
| Armas Nocturnas + aspectos | 6 armas con Ataque, Especial, Omega | 5 **Armas del Umbral**, 2 aspectos ×3 niveles cada una |
| Magia y movimientos Omega | Mantener pulsado gasta Magia; se rellena al acabar el encuentro | **Lucidez**, mismas reglas |
| Lanzamiento (círculo de atadura) | Inmoviliza enemigos | **Signo Arcano** (el Signo de los Primigenios); versión Omega |
| Esprint | Embestida + carrera | Igual: 0,17 s invulnerable, luego carrera al mantener |
| Dioses olímpicos + maldiciones | Zeus/Blitz, Afrodita/Débil, Deméter/Congelar, Hestia/Chamuscar, Poseidón, Hera/Enganche... | **Cthulhu**/Marea, **Hastur**/Locura, **Shub-Niggurath**/Semilla, **Nodens**/Sentencia, **Ithaqua**/Escarcha, **Cthugha**/Llama Viva |
| Ranuras de boon | Ataque, Especial, Lanzamiento, Esprint, Magia | Ataque, Especial, Signo, Esprint, Lucidez |
| Rarezas, Dúos, Legendarios, Infusiones | Común→Heroico, combinaciones de dos dioses, elementos | Igual (elementos Tierra/Agua/Aire/Fuego/Éter) |
| Granada de poder | Sube de nivel un boon | **Fruto Oscuro** |
| Martillo de Dédalo | Mejora de arma | **Fragmento de Eltdown** |
| Corazón de centauro | +Vida máxima | **Corazón de Gug** |
| Hermes | Velocidad y esquiva | **Gatos de Ulthar** |
| Caos | Maldición temporal + bendición | **Puertas de Azathoth** (cuestan vida) |
| Selene / Senda de las Estrellas | Hechizo que se carga con Magia gastada | **Ecos del Padre** (5 ecos, mejorables) |
| Caronte | Tienda | **Galeras Negras de Leng** |
| Cartas Arcanas + Capacidad | Mejoras permanentes con límite | **Grimorio de Keziah** (15 cartas) + Capacidad mental |
| Caldero (conjuros) | Desbloquear contenido | **Caldero de 'Umr at-Tawil** (21 rituales) |
| Juramento de lo Invisible (Miedo) | Dificultad opcional | **Juramento de lo Innombrable** (Pavor, 10 pactos) |
| Recuerdos (keepsakes) | Bonus equipable por regalo | **Reliquias** (7) |
| Familiares | Compañero con habilidades | **Ébano** (gato) y **Pipo** (zoog) |
| Néctar / relaciones | Regalos y diálogos | **Licor de Luna** y afinidad |
| Desafío a la Muerte | Vida extra | **Último Umbral** |
| Némesis | Rival recurrente | **Eco de Wilbur Whateley** |
| Cenizas, Psique, Huesos, Plata... | Recursos permanentes | Ceniza Estelar, Ensueño, Huesos de Ghoul, Plata Onírica, Polvo de Kadath, Licor de Luna, Lágrima de Yog |
| Modo Dios | Accesibilidad | Igual (20% + 2% por muerte, máx. 80%) |

## 2. Bucle de juego

1. **Umbral**: hablar, regalar, comprar rituales y cartas, elegir arma/aspecto, reliquia, familiar y pactos.
2. **Cruzar la Puerta**: incursión de 4 regiones × (8 salas + guardián).
3. En cada sala: oleadas → recompensa → elegir puerta (cada puerta muestra su recompensa).
4. Al morir: vuelves al Umbral conservando los recursos permanentes. Al ganar: epílogo y vuelta (la historia avanza con cada victoria; tras 3, el ritual «Abrir la Última Puerta» da el final verdadero).

## 3. Regiones

| Región | Enemigos | Élite | Guardián |
|---|---|---|---|
| El Bosque Encantado | Zoog, Necrófago, Ángel Descarnado | Gug | Richard Upton Pickman (excava, invoca necrófagos, pinta "retratos" de proyectiles) |
| R'lyeh Hundida | Profundo, Sacerdote de Dagón, Pólipo Volante | Engendro Estelar | Padre Dagón y Madre Hidra (el superviviente se enfurece) |
| La Meseta de Leng | Hombre de Leng, Bestia Lunar, Araña de Leng | Shantak | El Sumo Sacerdote (haces giratorios, Signo Amarillo que invierte tus controles) |
| La Corte de Azathoth | Mi-Go, Sabueso de Tindalos, Flautista Amorfo | Shoggoth (se divide) | Nyarlathotep en tres formas |

## 4. Dirección de arte

- Cámara 3/4 en diagonal (inclinación 52°, giro 45°) con FOV estrecho: el aspecto "diorama isométrico" de Hades.
- Sombreado *cel* con bandas duras, sombras teñidas de violeta, luz de borde y **contorno de tinta** de grosor constante en pantalla.
- Cada sala es una isla flotando sobre un **abismo cósmico animado** (nebulosa en espiral, estrellas y las esferas iridiscentes de Yog-Sothoth).
- Suelos pintados (losas Voronoi con grietas) con venas brillantes por región; bloom, viñeta, grano y aberración cromática al recibir daño.
- Retratos de diálogo pintados con campos de distancia (formas planas, sombreado interior, tinta).
- Paletas: Bosque (verde fosforescente), R'lyeh (turquesa abisal), Leng (ocre y oro), Azathoth (magenta y rojo), Umbral (violeta iridiscente).

## 5. Ampliar el juego

- **Nuevo boon**: añade una entrada en `Boons/BoonDB.cs` (la lambda `apply` modifica `PlayerModifiers` y engancha efectos).
- **Nuevo enemigo**: añade un `EnemyDef` en `Enemies/EnemyDB.cs` con sus `AttackDef` y un modelo en `Art/EnemyArt.cs`.
- **Nuevo jefe**: hereda de `Boss` (ver `Bosses/Boss.cs`) y registra sus movimientos con `M(...)`.
- **Nueva arma**: hereda de `Weapon` y añade su `WeaponDef` en `Weapons/Weapons.cs`.
- **Diálogos**: `Narrative/Narrative.cs` (marcas de género `{o}`, `{hijo}`, `{hermano}`...).
- **Arte con assets propios**: sustituye las funciones de `CharacterArt`/`EnemyArt` por prefabs; el resto del juego solo necesita un `ModelRig`.
