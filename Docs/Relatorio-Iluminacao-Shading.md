# Relatório — Iluminação, Shading e Depuração Visual

**Disciplina:** Computação Gráfica
**Projeto:** Mini Golf (Unity)
**Autor:** João Alexandre

---

## 1. Introdução

Este trabalho estende um jogo de mini golf em Unity com um conjunto de **ferramentas didáticas interativas** que demonstram, ao vivo e dentro do próprio jogo, os principais conceitos de iluminação e sombreamento (*shading*) estudados na disciplina. O objetivo não é apenas "deixar o jogo mais bonito", mas permitir **comparar** técnicas lado a lado e **visualizar** o que normalmente fica escondido no pipeline de renderização.

O projeto usa o **Built-in Render Pipeline** da Unity, com espaço de cor **Gamma**. Toda a interação acontece por um menu único, dividido em abas (**Iluminação**, **Shading** e **Debug**), aberto com a tecla `TAB`.

## 2. Arquitetura das ferramentas

As ferramentas foram implementadas como componentes independentes que **se instanciam sozinhos em tempo de execução** (via `RuntimeInitializeOnLoadMethod`), sem necessidade de configurar objetos na cena. Cada componente persiste entre as cenas (`DontDestroyOnLoad`).

| Script | Papel |
|--------|-------|
| `LightingPresetController` | Presets de iluminação (aba Iluminação) |
| `ShadingModelController` + `CGShadingModels.shader` | Modelos de sombreamento (aba Shading) |
| `DebugViewController` + `CGDebugViews.shader` | Modos de depuração visual (aba Debug) |
| `CGMenuHUD` | Menu único com abas |
| `CGCameraZoom` | Zoom da câmera (roda do mouse) |
| `CGMenuGuard` | Impede que cliques no menu movam a câmera/bola |

Como o jogo recria a bola e recarrega a cena a cada troca de fase, cada módulo expõe um método `NotifyLevelSpawned()` chamado por `LevelManager.SpawnLevel`, garantindo que os efeitos escolhidos sejam **reaplicados** na fase nova.

---

## 3. Módulo 1 — Iluminação

### 3.1 O modelo de iluminação

A cor final de um ponto de uma superfície é a soma de três contribuições clássicas:

```
Cor final = Ambiente + Difusa + Especular
```

- **Ambiente** — aproximação barata da luz indireta (a luz que "quica" no ambiente e chega de todos os lados). Sem ela, as regiões em sombra ficariam totalmente pretas.
- **Difusa (Lambert)** — depende do ângulo entre a normal da superfície `N` e a direção da luz `L`. Quanto mais a face "encara" a luz, mais clara fica:

  ```
  I_difusa = max(0, N · L)
  ```

- **Especular** — o brilho/reflexo, tratado em detalhe no Módulo 2.

Na Unity (Built-in), a componente ambiente é configurada em `RenderSettings` e pode operar em três modos:

- **Flat** — uma cor única para todo o ambiente. Resultado chapado, sem profundidade.
- **Trilight (gradiente)** — três cores (céu, equador e chão), simulando que o topo recebe luz do céu e a base recebe luz refletida do chão.
- **Skybox** — deriva a cor ambiente do skybox.

### 3.2 Tipos de fonte de luz

Foram implementados os três tipos fundamentais de luz, cada um demonstrando uma propriedade diferente:

| Tipo | Posição | Direção | Atenuação | Cone |
|------|:-------:|:-------:|:---------:|:----:|
| **Directional** | — | ✔ | — | — |
| **Point** | ✔ | — | ✔ | — |
| **Spot** | ✔ | ✔ | ✔ | ✔ |

- **Directional (direcional)** — simula uma fonte muito distante (o Sol). Os raios são **paralelos**, então importa apenas a **direção**, não a posição. Ilumina todos os objetos com a mesma intensidade, independentemente da distância.
- **Point (pontual)** — simula uma lâmpada. Tem **posição** no espaço e emite em **todas as direções**. Sua característica-chave é a **atenuação**: a intensidade cai com a distância. Fisicamente, a queda é proporcional a `1/d²` (lei do inverso do quadrado); a Unity aproxima esse comportamento com um parâmetro `range`, dentro do qual a luz vai suavemente a zero.
- **Spot (holofote)** — combina os dois anteriores: tem **posição**, **direção** e **atenuação**, e adiciona um **cone** de abertura (`spotAngle`). É o superconjunto dos outros dois.

### 3.3 Iluminação de três pontos

Um dos presets demonstra a técnica clássica de cinema/fotografia da **iluminação de três pontos**, aqui simplificada em duas luzes:

- **Key light (luz principal)** — a luz direcional forte que define o volume e projeta as sombras.
- **Fill light (luz de preenchimento)** — uma segunda luz, mais fraca, posicionada do **lado oposto** ao key. Sua função é **suavizar** as sombras: sem ela, o lado escuro do objeto ficaria em preto absoluto.

### 3.4 Sombras

As sombras são calculadas por **shadow mapping**: a cena é renderizada do ponto de vista da luz, guardando as distâncias em uma textura (o *shadow map*). Depois, para cada pixel visível, compara-se sua distância à luz com o valor guardado para decidir se ele está iluminado ou bloqueado.

Parâmetros explorados:

- **Tipo** — `Hard` (borda serrilhada e seca) vs `Soft` (borda suavizada, mais realista, porém mais custosa).
- **Strength (força)** — quão escura é a sombra (1 = preta; valores menores simulam dias nublados).
- **Resolution (resolução)** — o tamanho do shadow map. Resolução baixa produz bordas serrilhadas/pixeladas; alta produz bordas nítidas ao custo de desempenho.

Dois artefatos clássicos do shadow mapping merecem menção: o **shadow acne** (padrões de listras causados por erros de precisão ao comparar profundidades) e o **peter-panning** (a sombra "descola" do objeto). Ambos são controlados pelo parâmetro **bias**, que desloca ligeiramente a profundidade comparada.

### 3.5 Névoa (fog)

A névoa foi usada nos presets mais dramáticos para dar profundidade atmosférica. Utilizou-se o modo `ExponentialSquared`, em que a densidade da névoa cresce com o quadrado da distância — objetos distantes desaparecem gradualmente na cor da névoa.

### 3.6 Presets implementados

| Tecla | Preset | Demonstra |
|:-----:|--------|-----------|
| `1` | Baseline (Flat) | Ambiente chapado, base de comparação |
| `2` | Depth (Directional + Trilight) | Key light + ambiente gradiente |
| `3` | Dramatic (Key + Fill + Fog) | Iluminação de três pontos + névoa |
| `4` | Sunset | Contraste de temperatura (luz quente × ambiente frio) |
| `5` | Night (Point Light) | Atenuação de luz pontual |
| `6` | Stage (Spot Light) | Cone e abertura de holofote |
| `7` | Shadow Study (Hard, Low Res) | Sombra dura e de baixa resolução |

A tecla `B` alterna entre "antes/depois" (o estado original da cena e o preset ativo), e `N` avança para o próximo preset.

---

## 4. Módulo 2 — Modelos de Shading

Este módulo troca, **em tempo real**, o modelo de sombreamento aplicado à bola, permitindo comparar como cada técnica calcula a reflexão da luz. Os três modelos clássicos foram implementados num **shader próprio** (`CG/ShadingModels`), com a matemática explícita; o modelo PBR usa o shader `Standard` da Unity.

Vetores usados em todos os modelos:

- `N` — normal da superfície
- `L` — direção para a luz
- `V` — direção para a câmera (observador)

### 4.1 Lambert (difuso puro)

O modelo mais simples: só a componente difusa, sem brilho especular. A superfície parece fosca.

```
I = max(0, N · L)
```

### 4.2 Phong (especular por reflexão)

Adiciona o brilho especular. Reflete o vetor da luz em torno da normal (`R`) e compara com a direção da câmera:

```
R = reflect(-L, N)
I_especular = (max(0, R · V))^n
```

O expoente `n` (*shininess*) controla o tamanho do brilho: valores altos produzem um ponto de brilho pequeno e concentrado (superfície muito polida); valores baixos, um brilho grande e espalhado.

### 4.3 Blinn-Phong (especular por vetor *halfway*)

Variante mais eficiente do Phong. Em vez de calcular o vetor refletido `R`, usa o **vetor intermediário** (*halfway*) entre a luz e a câmera:

```
H = normalize(L + V)
I_especular = (max(0, N · H))^n
```

Por evitar a operação `reflect`, é mais barato de calcular — razão pela qual se tornou o padrão em renderização em tempo real. A distribuição do brilho é ligeiramente diferente da do Phong.

### 4.4 PBR (*Physically Based Rendering*)

Enquanto Phong e Blinn-Phong são modelos **empíricos** ("parecem certos"), o PBR é **baseado em física**: respeita a conservação de energia e descreve o material por parâmetros físicos:

- **Metallic** — quão metálico é o material.
- **Smoothness/Roughness** — quão polida é a superfície (define a nitidez do reflexo).

No PBR, o reflexo depende também do ambiente (skybox / *reflection probes*), o que o torna mais realista do que os modelos clássicos.

### 4.5 Comparação

| Modelo | Especular | Custo | Base |
|--------|-----------|-------|------|
| Lambert | Não | Baixo | Empírico |
| Phong | `(R·V)^n` | Médio | Empírico |
| Blinn-Phong | `(N·H)^n` | Baixo–médio | Empírico |
| PBR | Microfacetas | Alto | Físico |

Controle: a tecla `C` cicla entre os modelos; um *slider* ajusta o *shininess* (Phong/Blinn) ou *metallic/smoothness* (PBR).

---

## 5. Módulo 3 — Depuração Visual (Debug Views)

Este módulo revela o que normalmente fica escondido no pipeline. É a "prova visual" dos conceitos dos módulos anteriores.

- **Wireframe** — desenha apenas as **arestas** dos triângulos, expondo a malha (*mesh*) por baixo do material. Quanto mais triângulos, mais suave a superfície. Implementado com `GL.wireframe` ativado entre os eventos de pré e pós-renderização da câmera.
- **Normais** — mapeia o vetor **normal** de cada fragmento em cor (X→vermelho, Y→verde, Z→azul). Como a normal é a base do cálculo `N · L`, este modo mostra literalmente o `N` que alimenta toda a iluminação. Faces voltadas para direções diferentes aparecem com cores diferentes.
- **UVs** — mostra as **coordenadas de textura** (U→vermelho, V→verde), evidenciando como uma imagem 2D é mapeada sobre a malha 3D.
- **Depth** — visualiza a **profundidade** (distância de cada pixel à câmera) em tons de cinza: mais perto = mais claro. É o conteúdo do *depth buffer*, usado para decidir o que aparece na frente.

Os modos Normais, UVs e Depth usam um **shader de *replacement*** (`Camera.SetReplacementShader`), que substitui o shader de todos os objetos por um shader de depuração. Como não há material nesse caso, o modo ativo é passado por variáveis **globais** (`Shader.SetGlobalInt`).

**Conexão didática:** alternar entre o modo *Normais* e o shading *Phong* mostra que a cor das normais corresponde exatamente ao vetor `N` do termo `N · L` — unindo geometria, normal, iluminação e sombreamento numa só narrativa.

---

## 6. Recursos auxiliares

- **Zoom (roda do mouse)** — ajusta o *field of view* da câmera (15°–60°) para inspecionar de perto o material/textura da bola. O botão do meio do mouse reseta.
- **Guarda de menu** — os menus registram a área que ocupam na tela; o `InputManager` ignora cliques sobre elas, evitando que arrastar um *slider* mova a câmera.

## 7. Tabela de controles

| Tecla / ação | Função |
|--------------|--------|
| `TAB` | Abre/fecha o menu |
| `1`–`7` | Presets de iluminação |
| `N` | Próximo preset de luz |
| `B` | Antes/depois (luz) |
| `C` | Cicla modelo de shading |
| `V` | Cicla modo de depuração |
| Roda do mouse | Zoom |
| Botão do meio | Reseta o zoom |

## 8. Conclusão

As ferramentas transformam o jogo num laboratório interativo de Computação Gráfica, cobrindo o caminho completo do pipeline de renderização: **geometria (malha e normais) → modelo de iluminação (ambiente, difusa, especular) → tipos de fonte de luz → sombras → modelos de sombreamento**. A possibilidade de comparar técnicas lado a lado e de visualizar dados intermediários (normais, UVs, profundidade) reforça a compreensão dos conceitos teóricos.

### Possíveis extensões

- Câmera: comparação entre projeção **perspectiva** e **ortográfica**.
- **Texturas** e **normal mapping** (detalhe de iluminação sem geometria adicional).
- **Pós-processamento** (bloom, oclusão de ambiente, profundidade de campo).
- Iluminação **pré-calculada** (*lightmapping*) vs **tempo real**, e *light probes*.

## 9. Referências

- Documentação da Unity — *Lighting*, *Shadows*, *Writing Shaders* (Built-in Render Pipeline).
- Phong, B. T. (1975). *Illumination for Computer Generated Pictures*.
- Blinn, J. F. (1977). *Models of Light Reflection for Computer Synthesized Pictures*.
- Williams, L. (1978). *Casting Curved Shadows on Curved Surfaces* (shadow mapping).
