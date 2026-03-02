Img2Img{
    model: "grok-imagine-image"
 prompt: "The reference graybox image shows TWO GENDER-NEUTRAL POSE DUMMIES with zero age or gender information.
    FIRST reference (pose1.png) = MAXIMUM FIDELITY POSE REFERENCE with ZERO DEVIATION:
    - Supine dummy lies EXACTLY SUPINE on its BACK on a completely flat floor mat (NO pillow, NO cushion, NO raised surface whatsoever), head on left side of frame, face relaxed upward, legs straight and relaxed pointing right, left arm relaxed beside body, right arm relaxed beside body.
    - Kneeling dummy is KNEELING DEEP in lotus-style position on the RIGHT side of the supine dummy on the SAME flat floor mat, leaning far forward.
    - EXACT hand positions: kneeling dummy's LEFT hand on upper shoulder/chest area of supine dummy, RIGHT hand on upper thigh/hip area of supine dummy.
    - Keep 100% identical body proportions, joint angles, limb positions, hand contact points, spatial relationship and camera angle.

    Render:
    - The supine dummy as a woman in tasteful classical artistic nude style (elegant, serene, non-sexual fine-art nude, no explicit details).
    - The kneeling dummy as a man wearing only simple tight black athletic underwear (modest, non-sexual).

    SECOND reference (Gi2Yd.jpg) = EXACT ART STYLE: classical pencil sketch, intricate graphite & charcoal cross-hatching, dramatic chiaroscuro, deep shadows, bright highlights, visible paper grain, slight sepia tone on aged textured paper.

    Add powerful ethereal glowing white energy beam streaming from the kneeling man's fingertips into the woman's shoulder and chest, luminous rays and sparkling particles.
    Tasteful artistic masterpiece fine-art drawing, high contrast, no color, serene spiritual healing atmosphere, emotional."

    negativePrompt: "pillow, cushion, raised mat, both hands on chest, wrong hand placement, standing, extra fabric, shorts on woman, shirt, young woman with wrong pose, deformed anatomy, bad hands, explicit sexual, photorealistic, color, modern clothing, low detail"
    inputPose: "pose1.png"
    inputStick: "stick.png"
    inputStyle: "style.jpg"
    output: "pose1_<version>.png"
    image_strength: 0.99
    style_strength: 0.86
    guidance_scale: 9.8
    steps: 75
}

