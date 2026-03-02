Img2Img{
    model: "grok-imagine-image"
    /*prompt: "Precisely keep the exact pose, anatomy, proportions and composition from the first reference image. 
    Render the entire scene using ONLY the artistic style, colors, lighting, shading, texture and aesthetic from the second reference image. 
    Do not change or mix anything from the pose."*/

    /*prompt: "STRICTLY preserve the exact pose, anatomy, proportions, hand placements, leg positions and full spatial composition from the first reference image ONLY. 
Render the human figures as HIGHLY REALISTIC photorealistic people with detailed natural skin texture, realistic faces, natural hair, lifelike facial expressions, subtle skin details and realistic human anatomy. 
Render the entire scene using ONLY the artistic style, colors, lighting, shading, texture and aesthetic from the second reference image. 
Do not change or mix anything from the pose." */

    prompt: "STRICTLY preserve the EXACT pose, anatomy, proportions, hand placements, leg positions and full spatial composition from the 
        FIRST reference image ONLY. Do not change or reinterpret anything from the pose.
        Use the SECOND reference image EXCLUSIVELY for artistic style, colors, dramatic candle lighting, blue energy smoke, atmosphere and mood 
        of the entire scene.

        Use the THIRD reference image EXCLUSIVELY and precisely for clothing ONLY:
        - The male figure has short hair is completely shirtless with bare torso and wears ONLY small, tight white massage shorts / brief-style shorts that fully cover the genitals exactly as shown in the third image.
        - The female figure has long blond curly hair is topless with both breasts fully visible and uncovered. ONLY a small white towel neatly draped over her hips and pubic area EXACTLY as shown in the third image.
    Render both figures as HIGHLY REALISTIC photorealistic people with natural skin texture, realistic faces, natural hair and lifelike details. Do not add any extra clothing on upper body or lower body beyond what is shown in the third reference."

    negativePrompt: "tank top, singlet, t-shirt, shirt, sports bra, bra, top on female, clothing on upper body, covered breasts, shorts on female, long shorts, excessive clothing, too much clothing, clothed torso, mannequin, doll, plastic skin"
    inputPose: "pose1.png"
    inputStyle: "sacred.jpg"
    inputExtra: "clothing.jpg"
    output: "pose1_<version>.png"
    image_strength: 0.88
    style_strength: 0.78
    guidance_scale: 9.8
    steps: 75
    aspect_ratio: "16:9"
    resolution:"2k"
}

