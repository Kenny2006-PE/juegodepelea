import os
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.enum.text import PP_ALIGN
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_SHAPE

def create_deck():
    prs = Presentation()
    prs.slide_width = Inches(13.333)
    prs.slide_height = Inches(7.5)
    blank_layout = prs.slide_layouts[6] # Blank slide

    # Palette
    C_BG_DARK = RGBColor(14, 17, 24)        # #0E1118 Deep Charcoal
    C_BG_CARD = RGBColor(23, 27, 38)        # #171B26 Card background
    C_BG_CARD_LIGHT = RGBColor(32, 38, 54)  # #202636 Card highlight
    C_BORDER = RGBColor(45, 53, 72)         # #2D3548 Subtle border
    
    C_TANJIRO = RGBColor(0, 201, 136)       # #00C988 Emerald Cyan
    C_MADARA = RGBColor(255, 69, 58)        # #FF453A Crimson Red
    C_GOLD = RGBColor(255, 209, 102)        # #FFD166 Radiant Gold
    C_CYAN = RGBColor(56, 189, 248)         # #38BDF8 Sky Cyan
    
    C_TEXT_WHITE = RGBColor(245, 247, 250)  # #F5F7FA Main Title
    C_TEXT_MUTED = RGBColor(156, 163, 175)  # #9CA3AF Body text
    C_TEXT_LIGHT = RGBColor(209, 213, 219)  # #D1D5DB Subtitle / item text

    screenshots_dir = "Screenshots"

    def bg(slide, color=C_BG_DARK):
        rect = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, 0, 0, Inches(13.333), Inches(7.5))
        rect.fill.solid()
        rect.fill.fore_color.rgb = color
        rect.line.fill.background()
        return rect

    def header(slide, tag_text, title_text, tag_color=C_GOLD):
        # Tag
        tb_tag = slide.shapes.add_textbox(Inches(0.8), Inches(0.4), Inches(11.733), Inches(0.35))
        tf_tag = tb_tag.text_frame
        tf_tag.word_wrap = True
        tf_tag.margin_left = tf_tag.margin_right = tf_tag.margin_top = tf_tag.margin_bottom = 0
        p_tag = tf_tag.paragraphs[0]
        p_tag.text = tag_text.upper()
        p_tag.font.size = Pt(11)
        p_tag.font.bold = True
        p_tag.font.color.rgb = tag_color

        # Title
        tb_title = slide.shapes.add_textbox(Inches(0.8), Inches(0.75), Inches(11.733), Inches(0.65))
        tf_title = tb_title.text_frame
        tf_title.word_wrap = True
        tf_title.margin_left = tf_title.margin_right = tf_title.margin_top = tf_title.margin_bottom = 0
        p_title = tf_title.paragraphs[0]
        p_title.text = title_text
        p_title.font.size = Pt(24)
        p_title.font.bold = True
        p_title.font.color.rgb = C_TEXT_WHITE

        # Divider line
        div = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0.8), Inches(1.45), Inches(11.733), Inches(0.02))
        div.fill.solid()
        div.fill.fore_color.rgb = C_BORDER
        div.line.fill.background()

    def card(slide, left, top, width, height, bg_color=C_BG_CARD, border_color=C_BORDER):
        c = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, Inches(left), Inches(top), Inches(width), Inches(height))
        c.fill.solid()
        c.fill.fore_color.rgb = bg_color
        if border_color:
            c.line.color.rgb = border_color
            c.line.width = Pt(1.2)
        else:
            c.line.fill.background()
        return c

    def image_box(slide, filename, left, top, width, height, border_color=C_BORDER):
        path = os.path.join(screenshots_dir, filename)
        if os.path.exists(path):
            # Frame/border
            if border_color:
                pad = 0.03
                frame = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(left - pad), Inches(top - pad), Inches(width + pad*2), Inches(height + pad*2))
                frame.fill.solid()
                frame.fill.fore_color.rgb = border_color
                frame.line.fill.background()
            pic = slide.shapes.add_picture(path, Inches(left), Inches(top), Inches(width), Inches(height))
            return pic
        return None

    # =========================================================================
    # SLIDE 1: PORTADA PRINCIPAL (HERO SLIDE)
    # =========================================================================
    s1 = prs.slides.add_slide(blank_layout)
    bg(s1)
    
    # Hero Background Shot on Right Side
    image_box(s1, "05_Duelo_Epico_Kick_vs_Block.png", 6.2, 0.8, 6.4, 5.9, border_color=C_BORDER)

    # Left Content Box
    tb1 = s1.shapes.add_textbox(Inches(0.8), Inches(1.3), Inches(5.1), Inches(4.8))
    tf1 = tb1.text_frame
    tf1.word_wrap = True
    
    p = tf1.paragraphs[0]
    p.text = "PROYECTO DE DESARROLLO DE VIDEOJUEGOS"
    p.font.size = Pt(12)
    p.font.bold = True
    p.font.color.rgb = C_GOLD
    p.space_after = Pt(14)

    p = tf1.add_paragraph()
    p.text = "TANJIRO VS MADARA"
    p.font.size = Pt(38)
    p.font.bold = True
    p.font.color.rgb = C_TEXT_WHITE
    p.space_after = Pt(6)

    p = tf1.add_paragraph()
    p.text = "Arena Combat 2.5D en Unity Engine"
    p.font.size = Pt(20)
    p.font.bold = True
    p.font.color.rgb = C_CYAN
    p.space_after = Pt(18)

    p = tf1.add_paragraph()
    p.text = "Desarrollo completo de un juego de lucha 1v1 con arquitectura limpia en C#, animaciones Humanoid Mixamo, sistema de combate por hitboxes/hurtboxes, IA predictiva por dificultades y soporte multijugador local."
    p.font.size = Pt(13)
    p.font.color.rgb = C_TEXT_MUTED
    p.space_after = Pt(24)

    # Tech Stack Pills
    p = tf1.add_paragraph()
    p.text = "Motor: Unity 2022.3 LTS  •  Lenguaje: C#  •  Gráficos: 3D / 2.5D  •  Mixamo Humanoid"
    p.font.size = Pt(11)
    p.font.bold = True
    p.font.color.rgb = C_TEXT_LIGHT

    # =========================================================================
    # SLIDE 2: FICHA TÉCNICA Y RESUMEN EJECUTIVO
    # =========================================================================
    s2 = prs.slides.add_slide(blank_layout)
    bg(s2)
    header(s2, "VISIÓN GENERAL DEL PROYECTO", "Ficha Técnica y Objetivos de Diseño")

    # 4 Metric Cards Across Top
    metrics = [
        ("GÉNERO PRINCIPAL", "Lucha 1v1 Arena 2.5D", C_CYAN),
        ("MOTOR & CÓDIGO", "Unity 2022.3 LTS (C#)", C_GOLD),
        ("MODOS DE JUEGO", "1P vs CPU / 1P vs 2P Local", C_TANJIRO),
        ("DISTRIBUCIÓN", "Windows 64-bit Standalone", C_MADARA)
    ]
    card_w = 2.75
    gap = 0.24
    for i, (m_title, m_val, m_col) in enumerate(metrics):
        cx = 0.8 + i * (card_w + gap)
        card(s2, cx, 1.7, card_w, 1.15, C_BG_CARD, m_col)
        tb = s2.shapes.add_textbox(Inches(cx + 0.15), Inches(1.8), Inches(card_w - 0.3), Inches(0.95))
        tf = tb.text_frame
        tf.word_wrap = True
        p0 = tf.paragraphs[0]
        p0.text = m_title
        p0.font.size = Pt(10)
        p0.font.bold = True
        p0.font.color.rgb = m_col
        p1 = tf.add_paragraph()
        p1.text = m_val
        p1.font.size = Pt(13)
        p1.font.bold = True
        p1.font.color.rgb = C_TEXT_WHITE

    # Left Column: Key Features
    card(s2, 0.8, 3.1, 5.7, 3.9)
    tb = s2.shapes.add_textbox(Inches(1.05), Inches(3.25), Inches(5.2), Inches(3.6))
    tf = tb.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = "Pilares de la Experiencia de Juego"
    p.font.size = Pt(16)
    p.font.bold = True
    p.font.color.rgb = C_GOLD
    p.space_after = Pt(12)

    features = [
        ("Autenticidad de Personajes", "Modelos 3D y sets de 10 animaciones reales de Mixamo adaptadas con precisión."),
        ("Profundidad de Combate", "Golpes ligeros, patadas pesadas con derribo, combos encadenados y guardia frontal con chip damage (85%)."),
        ("Dualidad de Entrada", "Partidas individuales contra IA táctica o combates multijugador 1v1 local en el mismo teclado."),
        ("Cámara Cinematográfica", "Seguimiento dinámico que ajusta posición y zoom automáticamente según la distancia de los luchadores.")
    ]
    for title, desc in features:
        p = tf.add_paragraph()
        p.text = f"•  {title}: "
        p.font.bold = True
        p.font.size = Pt(12)
        p.font.color.rgb = C_TEXT_LIGHT
        run = p.add_run()
        run.text = desc
        run.font.bold = False
        run.font.color.rgb = C_TEXT_MUTED
        p.space_after = Pt(8)

    # Right Column: Combat Screen Preview
    image_box(s2, "02_Combate_Arena_HUD.png", 6.8, 3.1, 5.733, 3.9, border_color=C_BORDER)

    # =========================================================================
    # SLIDE 3: ROSTER Y PERSONAJES JUGABLES
    # =========================================================================
    s3 = prs.slides.add_slide(blank_layout)
    bg(s3)
    header(s3, "ROSTER Y CONCEPT ART", "Tanjiro Kamado vs Madara Uchiha")

    # Left Fighter: Tanjiro
    card(s3, 0.8, 1.7, 5.7, 5.3, C_BG_CARD, C_TANJIRO)
    image_box(s3, "03_Tanjiro_CloseUp.png", 1.05, 1.9, 5.2, 2.7, border_color=C_TANJIRO)
    tb = s3.shapes.add_textbox(Inches(1.05), Inches(4.75), Inches(5.2), Inches(2.1))
    tf = tb.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = "TANJIRO KAMADO — Espadachín de la Respiración"
    p.font.size = Pt(15)
    p.font.bold = True
    p.font.color.rgb = C_TANJIRO
    p.space_after = Pt(6)

    t_points = [
        ("Estilo de Pelea:", "Ágil, presionante, ideal para jugadores de ritmo rápido."),
        ("Ataque Ligero:", "Jab Cross veloz (8 pts daño, hitstun instantáneo)."),
        ("Ataque Fuerte:", "Roundhouse Kick de alto impacto (18 pts + Knockdown)."),
        ("Defensa:", "Center Block compacto para anular combos rivales.")
    ]
    for lbl, val in t_points:
        p = tf.add_paragraph()
        p.text = f"• {lbl} "
        p.font.bold = True
        p.font.size = Pt(11)
        p.font.color.rgb = C_TEXT_LIGHT
        run = p.add_run()
        run.text = val
        run.font.bold = False
        run.font.color.rgb = C_TEXT_MUTED
        p.space_after = Pt(3)

    # Right Fighter: Madara
    card(s3, 6.833, 1.7, 5.7, 5.3, C_BG_CARD, C_MADARA)
    image_box(s3, "04_Madara_CloseUp.png", 7.083, 1.9, 5.2, 2.7, border_color=C_MADARA)
    tb = s3.shapes.add_textbox(Inches(7.083), Inches(4.75), Inches(5.2), Inches(2.1))
    tf = tb.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = "MADARA UCHIHA — Líder Legendario del Sharingan"
    p.font.size = Pt(15)
    p.font.bold = True
    p.font.color.rgb = C_MADARA
    p.space_after = Pt(6)

    m_points = [
        ("Estilo de Pelea:", "Dominante, castigador de errores, control de área."),
        ("Ataque Ligero:", "Jab Cross preciso para frenar acometidas rivales."),
        ("Ataque Fuerte:", "Roundhouse Kick contundente con empuje masivo."),
        ("Defensa:", "Bloqueo impenetrable que desgasta al oponente.")
    ]
    for lbl, val in m_points:
        p = tf.add_paragraph()
        p.text = f"• {lbl} "
        p.font.bold = True
        p.font.size = Pt(11)
        p.font.color.rgb = C_TEXT_LIGHT
        run = p.add_run()
        run.text = val
        run.font.bold = False
        run.font.color.rgb = C_TEXT_MUTED
        p.space_after = Pt(3)

    # =========================================================================
    # SLIDE 4: ARQUITECTURA DEL SOFTWARE (CLEAN ARCHITECTURE C#)
    # =========================================================================
    s4 = prs.slides.add_slide(blank_layout)
    bg(s4)
    header(s4, "INGENIERÍA DE SOFTWARE", "Arquitectura Desacoplada y Modular")

    # 4 Architecture Columns
    modules = [
        ("1. NÚCLEO (CORE)", C_GOLD, [
            ("GameManager.cs", "Controlador central de fases de partida, rondas, timer y spawns."),
            ("MatchSettings.cs", "Configuración global de jugadores, identidades, colores y dificultad."),
            ("FighterState.cs", "Máquina de estados de combate finita (Idle, Moving, Attacking, Hitstun, KO).")
        ]),
        ("2. ENTIDADES (FIGHTER)", C_CYAN, [
            ("FighterController.cs", "Lógica motriz, combate, combos, orientación mutua automática."),
            ("IFighterInput.cs", "Interfaz abstracta para aislar controles humanos de la IA."),
            ("FighterAIController.cs", "Cerebro reactivo con evaluación de distancias y dificultad.")
        ]),
        ("3. COMBATE FÍSICO", C_TANJIRO, [
            ("Hitbox.cs / Hurtbox.cs", "Detección precisa mediante triggers activos por animación."),
            ("AttackData.cs", "Estructura de datos con daño, frames activos y tipo de reacción."),
            ("HealthSystem.cs", "Gestión de puntos de vida, chip damage (85%) y mitigación.")
        ]),
        ("4. INTERFAZ Y VFX", C_MADARA, [
            ("UIManager.cs", "Barras de vida interpoladas (Lerp), medallón de tiempo, alertas."),
            ("MenuManager.cs", "Selector de modo, selección de personaje P1/P2 y dificultad."),
            ("FightingCamera.cs", "Cámara con SmoothDamp y encuadre dinámico de luchadores.")
        ])
    ]

    col_w = 2.75
    col_gap = 0.24
    for i, (m_title, m_col, classes) in enumerate(modules):
        cx = 0.8 + i * (col_w + col_gap)
        card(s4, cx, 1.7, col_w, 5.3, C_BG_CARD, m_col)
        tb = s4.shapes.add_textbox(Inches(cx + 0.15), Inches(1.85), Inches(col_w - 0.3), Inches(5.0))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = m_title
        p.font.size = Pt(13)
        p.font.bold = True
        p.font.color.rgb = m_col
        p.space_after = Pt(14)

        for c_name, c_desc in classes:
            p = tf.add_paragraph()
            p.text = c_name
            p.font.bold = True
            p.font.size = Pt(11)
            p.font.color.rgb = C_TEXT_WHITE
            p.space_after = Pt(2)

            p_desc = tf.add_paragraph()
            p_desc.text = c_desc
            p_desc.font.size = Pt(10)
            p_desc.font.color.rgb = C_TEXT_MUTED
            p_desc.space_after = Pt(12)

    # =========================================================================
    # SLIDE 5: ANIMATOR CONTROLLER & RIG HUMANOID
    # =========================================================================
    s5 = prs.slides.add_slide(blank_layout)
    bg(s5)
    header(s5, "PIPELINE DE ANIMACIÓN", "Rig Humanoid y Animator Override Controller")

    # Left Column: Animator Structure
    card(s5, 0.8, 1.7, 5.7, 5.3)
    tb = s5.shapes.add_textbox(Inches(1.05), Inches(1.85), Inches(5.2), Inches(5.0))
    tf = tb.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = "Estructura de la Máquina de Estados"
    p.font.size = Pt(16)
    p.font.bold = True
    p.font.color.rgb = C_CYAN
    p.space_after = Pt(12)

    anim_items = [
        ("BaseFighterAnimatorController", "Controlador maestro estructurado para Tanjiro con parámetros Speed (float), isBlocking (bool), Jab (trigger), Kick (trigger), LightHit (trigger), Knockdown (trigger) y KO (trigger)."),
        ("MadaraAnimatorOverride", "Override Controller que hereda exactamente la misma lógica de estados sin duplicar transiciones, mapeando los clips equivalentes de Madara."),
        ("Secuencia de Derribo y Levantada", "Knockdown dispara Stunned en el suelo ➔ transición automática por Exit Time a Getting Up ➔ retorno seguro a Fighting Idle con invulnerabilidad temporal."),
        ("Sincronización de Animaciones", "Configuración In Place en todos los FBX de Mixamo, asegurando que el movimiento espacial sea gobernado por código físico y no por Root Motion impreciso.")
    ]
    for a_title, a_desc in anim_items:
        p = tf.add_paragraph()
        p.text = f"• {a_title}: "
        p.font.bold = True
        p.font.size = Pt(12)
        p.font.color.rgb = C_TEXT_LIGHT
        run = p.add_run()
        run.text = a_desc
        run.font.bold = False
        run.font.color.rgb = C_TEXT_MUTED
        p.space_after = Pt(8)

    # Right Column: Visual Shot
    image_box(s5, "06_Impacto_Jab_Cross.png", 6.8, 1.7, 5.733, 5.3, border_color=C_BORDER)

    # =========================================================================
    # SLIDE 6: SISTEMA DE COMBATE: HITBOXES, HURTBOXES Y BLOQUEO
    # =========================================================================
    s6 = prs.slides.add_slide(blank_layout)
    bg(s6)
    header(s6, "MECÁNICAS DE COMBATE", "Hitboxes, Hurtboxes, Daño y Mitigación")

    # 3 Combat Cards
    c_cards = [
        ("JAB CROSS (LIGERO)", C_CYAN, [
            ("Daño Base:", "8 puntos"),
            ("Alcance:", "Corto (Puño frontal)"),
            ("Efecto:", "Hitstun inmediato leve (Head Hit)"),
            ("Rol Táctico:", "Interrumpir avances y abrir combos.")
        ]),
        ("ROUNDHOUSE KICK (FUERTE)", C_MADARA, [
            ("Daño Base:", "18 puntos"),
            ("Alcance:", "Medio-Largo (Patada circular)"),
            ("Efecto:", "Empuje masivo y derribo al suelo (Knockdown)"),
            ("Rol Táctico:", "Remate de combo o castigo a distancia.")
        ]),
        ("CENTER BLOCK (DEFENSA)", C_GOLD, [
            ("Mitigación:", "85% de reducción (Chip Damage)"),
            ("Hitstun:", "Completamente inmunizado"),
            ("Anti-Derribo:", "Evita caer al suelo contra patadas"),
            ("Feedback:", "Destello visual y sonido metálico.")
        ])
    ]
    card_w = 3.75
    gap = 0.24
    for i, (c_title, c_col, points) in enumerate(c_cards):
        cx = 0.8 + i * (card_w + gap)
        card(s6, cx, 1.7, card_w, 2.5, C_BG_CARD, c_col)
        tb = s6.shapes.add_textbox(Inches(cx + 0.15), Inches(1.85), Inches(card_w - 0.3), Inches(2.2))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = c_title
        p.font.size = Pt(13)
        p.font.bold = True
        p.font.color.rgb = c_col
        p.space_after = Pt(8)

        for p_lbl, p_val in points:
            p = tf.add_paragraph()
            p.text = f"• {p_lbl} "
            p.font.bold = True
            p.font.size = Pt(11)
            p.font.color.rgb = C_TEXT_LIGHT
            run = p.add_run()
            run.text = p_val
            run.font.bold = False
            run.font.color.rgb = C_TEXT_MUTED
            p.space_after = Pt(2)

    # Bottom Full Width Arena Combat Preview
    image_box(s6, "05_Duelo_Epico_Kick_vs_Block.png", 0.8, 4.45, 11.733, 2.55, border_color=C_BORDER)

    # =========================================================================
    # SLIDE 7: INTELIGENCIA ARTIFICIAL TÁCTICA (FighterAIController)
    # =========================================================================
    s7 = prs.slides.add_slide(blank_layout)
    bg(s7)
    header(s7, "COMPORTAMIENTO INTELIGENTE", "Inteligencia Artificial Reactiva y Dificultad")

    # Left: AI Strategy Cards
    diffs = [
        ("NIVEL FÁCIL (PRINCIPIANTE)", C_CYAN, "Cadencia de ataque pausada (1.2s - 2.0s). Rango de bloqueo del 20% al detectar ataques enemigos. Combos poco frecuentes; ideal para familiarizarse con los controles."),
        ("NIVEL NORMAL (EQUILIBRADO)", C_GOLD, "Reacción táctica (0.6s - 1.2s). Bloqueo inteligente al 50%. Mantiene distancia óptima, conecta combos Jab ➔ Kick y aprovecha aperturas en la guardia del rival."),
        ("NIVEL DIFÍCIL (EXPERTO)", C_MADARA, "Respuesta instantánea (0.3s - 0.6s). 80% de probabilidad de bloqueo reactivo. Presión constante, encadenamiento agresivo de combos y castigo riguroso de errores.")
    ]
    card_h = 1.6
    for i, (d_name, d_col, d_desc) in enumerate(diffs):
        cy = 1.7 + i * (card_h + 0.2)
        card(s7, 0.8, cy, 6.2, card_h, C_BG_CARD, d_col)
        tb = s7.shapes.add_textbox(Inches(1.0), Inches(cy + 0.12), Inches(5.8), Inches(card_h - 0.24))
        tf = tb.text_frame
        tf.word_wrap = True
        p = tf.paragraphs[0]
        p.text = d_name
        p.font.size = Pt(13)
        p.font.bold = True
        p.font.color.rgb = d_col
        p.space_after = Pt(4)
        p = tf.add_paragraph()
        p.text = d_desc
        p.font.size = Pt(11)
        p.font.color.rgb = C_TEXT_MUTED

    # Right: Close-up Duel
    image_box(s7, "04_Madara_CloseUp.png", 7.3, 1.7, 5.233, 5.3, border_color=C_BORDER)

    # =========================================================================
    # SLIDE 8: DISEÑO DE INTERFAZ DE USUARIO (UI / UX)
    # =========================================================================
    s8 = prs.slides.add_slide(blank_layout)
    bg(s8)
    header(s8, "EXPERIENCIA DE USUARIO (UI / UX)", "Menú Principal y HUD de Combate Competitivo")

    # Left: Menu Preview
    image_box(s8, "01_Menu_Principal.png", 0.8, 1.7, 5.7, 3.4, border_color=C_BORDER)
    card(s8, 0.8, 5.25, 5.7, 1.75)
    tb = s8.shapes.add_textbox(Inches(0.95), Inches(5.35), Inches(5.4), Inches(1.5))
    tf = tb.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = "Menú Principal y Selección Dinámica"
    p.font.size = Pt(13)
    p.font.bold = True
    p.font.color.rgb = C_GOLD
    p.space_after = Pt(3)
    p = tf.add_paragraph()
    p.text = "• Selección fluida de modos de juego (1P vs CPU / 1P vs 2P Local).\n• Asignación libre de personajes para Jugador 1 y Jugador 2.\n• Selector interactivo de dificultad con previsualización en tiempo real."
    p.font.size = Pt(10.5)
    p.font.color.rgb = C_TEXT_MUTED

    # Right: HUD Preview
    image_box(s8, "02_Combate_Arena_HUD.png", 6.833, 1.7, 5.7, 3.4, border_color=C_BORDER)
    card(s8, 6.833, 5.25, 5.7, 1.75)
    tb = s8.shapes.add_textbox(Inches(6.983), Inches(5.35), Inches(5.4), Inches(1.5))
    tf = tb.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = "HUD de Combate Competitivo"
    p.font.size = Pt(13)
    p.font.bold = True
    p.font.color.rgb = C_CYAN
    p.space_after = Pt(3)
    p = tf.add_paragraph()
    p.text = "• Barras de salud duales con interpolación suave (Smooth Lerp).\n• Medallón central dorado con temporizador dinámico de 60 segundos.\n• Sistema de diamantes (Pips) para contabilizar rondas ganadas (Bo3)."
    p.font.size = Pt(10.5)
    p.font.color.rgb = C_TEXT_MUTED

    # =========================================================================
    # SLIDE 9: CÁMARA DINÁMICA Y EFECTOS DE FEEDBACK (VFX)
    # =========================================================================
    s9 = prs.slides.add_slide(blank_layout)
    bg(s9)
    header(s9, "FEEDBACK SENSORIAL Y JUGABILIDAD", "Cámara Cinemática e Impacto Visual")

    # Left Column: Features
    card(s9, 0.8, 1.7, 5.7, 5.3)
    tb = s9.shapes.add_textbox(Inches(1.05), Inches(1.85), Inches(5.2), Inches(5.0))
    tf = tb.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = "Técnicas de Retroalimentación de Impacto"
    p.font.size = Pt(15)
    p.font.bold = True
    p.font.color.rgb = C_GOLD
    p.space_after = Pt(12)

    feedback_items = [
        ("FightingCamera (Encuadre Dinámico)", "Calcula en cada fotograma el punto medio exacto entre ambos luchadores y aplica SmoothDamp en posición y zoom. Mantiene a ambos personajes siempre en pantalla sin cortes."),
        ("Camera Shake (Sacudida de Cámara)", "Micro-temblor de cámara activado al conectar golpes pesados o derribos para comunicar contundencia física."),
        ("Partículas Procedurales de Chispas", "Sparks Particle System generado en el punto exacto de colisión de las hitboxes durante golpes directos."),
        ("Efecto Destello de Guardia", "Emisión luminosa y amortiguación física al golpear contra un luchador en guardia activa (Center Block)."),
        ("Orientación Mutua en Tiempo Real", "Ambos luchadores calculan su posición relativa en el eje X para rotar y mirarse fijamente de manera instantánea.")
    ]
    for fb_title, fb_desc in feedback_items:
        p = tf.add_paragraph()
        p.text = f"• {fb_title}: "
        p.font.bold = True
        p.font.size = Pt(11)
        p.font.color.rgb = C_TEXT_LIGHT
        run = p.add_run()
        run.text = fb_desc
        run.font.bold = False
        run.font.color.rgb = C_TEXT_MUTED
        p.space_after = Pt(6)

    # Right Column: Visual Close Up Shot
    image_box(s9, "03_Tanjiro_CloseUp.png", 6.8, 1.7, 5.733, 5.3, border_color=C_BORDER)

    # =========================================================================
    # SLIDE 10: DISTRIBUCIÓN, CONTROLES Y PORTABILIDAD USB
    # =========================================================================
    s10 = prs.slides.add_slide(blank_layout)
    bg(s10)
    header(s10, "DESPLIEGUE Y CONTROLES", "Empaquetado Standalone y Portabilidad USB")

    # Left: Controls Card
    card(s10, 0.8, 1.7, 6.2, 5.3, C_BG_CARD, C_GOLD)
    tb = s10.shapes.add_textbox(Inches(1.05), Inches(1.85), Inches(5.7), Inches(5.0))
    tf = tb.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = "Esquema de Controles por Teclado"
    p.font.size = Pt(16)
    p.font.bold = True
    p.font.color.rgb = C_GOLD
    p.space_after = Pt(10)

    p = tf.add_paragraph()
    p.text = "JUGADOR 1 (Lado Izquierdo):"
    p.font.bold = True
    p.font.size = Pt(12)
    p.font.color.rgb = C_TANJIRO
    p.space_after = Pt(2)
    p = tf.add_paragraph()
    p.text = "• Moverse: A (Izquierda) / D (Derecha)\n• Bloqueo Frontal: S\n• Golpe Ligero (Jab Cross): F\n• Patada Fuerte / Derribo (Roundhouse Kick): G"
    p.font.size = Pt(11)
    p.font.color.rgb = C_TEXT_MUTED
    p.space_after = Pt(10)

    p = tf.add_paragraph()
    p.text = "JUGADOR 2 (Lado Derecho - Modo Local):"
    p.font.bold = True
    p.font.size = Pt(12)
    p.font.color.rgb = C_MADARA
    p.space_after = Pt(2)
    p = tf.add_paragraph()
    p.text = "• Moverse: Flechas ← (Izquierda) / → (Derecha)\n• Bloqueo Frontal: Flecha ↓\n• Golpe Ligero (Jab Cross): J o Numpad 1\n• Patada Fuerte / Derribo (Roundhouse Kick): K o Numpad 2"
    p.font.size = Pt(11)
    p.font.color.rgb = C_TEXT_MUTED
    p.space_after = Pt(10)

    p = tf.add_paragraph()
    p.text = "TECLAS GENERALES:"
    p.font.bold = True
    p.font.size = Pt(12)
    p.font.color.rgb = C_CYAN
    p.space_after = Pt(2)
    p = tf.add_paragraph()
    p.text = "• Escape: Pausa / Retorno al menú en pelea; Salir del juego en el menú."
    p.font.size = Pt(11)
    p.font.color.rgb = C_TEXT_MUTED

    # Right: Packaging Card
    card(s10, 7.3, 1.7, 5.233, 5.3)
    tb = s10.shapes.add_textbox(Inches(7.55), Inches(1.85), Inches(4.7), Inches(5.0))
    tf = tb.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = "Portabilidad USB Plug & Play"
    p.font.size = Pt(16)
    p.font.bold = True
    p.font.color.rgb = C_CYAN
    p.space_after = Pt(12)

    dist_items = [
        ("Build Independiente:", "Compilado como ejecutable Windows 64-bit que no requiere tener Unity ni software adicional instalado."),
        ("Empaquetado Listo para USB:", "Generado en Tanjiro_vs_Madara_Juego.rar (34 MB) y .zip para facilitar su copia y ejecución directa."),
        ("Instrucciones Incluidas:", "Archivos LEEME_INSTRUCCIONES.txt y COMO_JUGAR.txt dentro de la carpeta con la guía rápida de usuario."),
        ("Rendimiento:", "Tasa fluida de 60 FPS estables gracias a la optimización de recursos y animaciones Humanoid.")
    ]
    for d_lbl, d_val in dist_items:
        p = tf.add_paragraph()
        p.text = f"• {d_lbl} "
        p.font.bold = True
        p.font.size = Pt(12)
        p.font.color.rgb = C_TEXT_LIGHT
        run = p.add_run()
        run.text = d_val
        run.font.bold = False
        run.font.color.rgb = C_TEXT_MUTED
        p.space_after = Pt(8)

    # =========================================================================
    # SLIDE 11: CONCLUSIONES Y TRABAJO FUTURO (CIERRE & Q&A)
    # =========================================================================
    s11 = prs.slides.add_slide(blank_layout)
    bg(s11)
    header(s11, "EVALUACIÓN Y HORIZONTE", "Conclusiones del Proyecto y Próximos Pasos")

    # Left: Achievements
    card(s11, 0.8, 1.7, 5.7, 5.3, C_BG_CARD, C_TANJIRO)
    tb = s11.shapes.add_textbox(Inches(1.05), Inches(1.85), Inches(5.2), Inches(5.0))
    tf = tb.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = "Logros Principales Alcanzados"
    p.font.size = Pt(16)
    p.font.bold = True
    p.font.color.rgb = C_TANJIRO
    p.space_after = Pt(12)

    achievements = [
        ("Arquitectura Limpia:", "Desacoplamiento total entre inputs físicos, cerebro de IA y controlador de combate."),
        ("Reutilización de Recursos:", "Integración de Animator Override Controllers para compartir la máquina de estados entre personajes."),
        ("Sensación de Impacto (Game Feel):", "Combos reactivos, mitigación con guardia frontal y derribos al suelo con invulnerabilidad temporal."),
        ("Versatilidad de Partida:", "Soporte completo tanto para práctica individual como para duelo competitivo de 2 jugadores.")
    ]
    for a_lbl, a_val in achievements:
        p = tf.add_paragraph()
        p.text = f"✔ {a_lbl} "
        p.font.bold = True
        p.font.size = Pt(12)
        p.font.color.rgb = C_TEXT_LIGHT
        run = p.add_run()
        run.text = a_val
        run.font.bold = False
        run.font.color.rgb = C_TEXT_MUTED
        p.space_after = Pt(10)

    # Right: Future Roadmap & Q&A
    card(s11, 6.833, 1.7, 5.7, 5.3, C_BG_CARD, C_GOLD)
    tb = s11.shapes.add_textbox(Inches(7.083), Inches(1.85), Inches(5.2), Inches(5.0))
    tf = tb.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = "Líneas de Trabajo Futuro"
    p.font.size = Pt(16)
    p.font.bold = True
    p.font.color.rgb = C_GOLD
    p.space_after = Pt(12)

    roadmap = [
        ("Ataques Especiales (Ultimate):", "Implementación de barras de energía (Super Meter) para desatar el Hinokami Kagura de Tanjiro y el Susanoo de Madara."),
        ("Soporte para Mandos (Gamepads):", "Integración con mandos de Xbox y PlayStation mediante el nuevo Unity Input System."),
        ("Expansión del Roster:", "Incorporación de nuevos luchadores icónicos (Zoro, Sasuke, Gojo)."),
        ("Escenarios Dinámicos:", "Arenas con destrucción parcial e iluminación volumétrica en URP.")
    ]
    for r_lbl, r_val in roadmap:
        p = tf.add_paragraph()
        p.text = f"➔ {r_lbl} "
        p.font.bold = True
        p.font.size = Pt(12)
        p.font.color.rgb = C_TEXT_LIGHT
        run = p.add_run()
        run.text = r_val
        run.font.bold = False
        run.font.color.rgb = C_TEXT_MUTED
        p.space_after = Pt(8)

    # Questions Banner
    p = tf.add_paragraph()
    p.text = "\n¿PREGUNTAS O COMENTARIOS?"
    p.font.size = Pt(14)
    p.font.bold = True
    p.font.color.rgb = C_CYAN
    p.alignment = PP_ALIGN.CENTER

    # Save
    out_desktop = "C:\\Users\\PC\\Desktop\\Tanjiro_vs_Madara_Presentacion.pptx"
    out_local = "Tanjiro_vs_Madara_Presentacion.pptx"
    prs.save(out_desktop)
    prs.save(out_local)
    print(f"Presentación guardada exitosamente en:\n- {out_desktop}\n- {out_local}")

if __name__ == "__main__":
    create_deck()
