import os
import base64
from xai_sdk import Client
from xai_sdk.chat import user, system, image


def local_image_to_data_uri(path: str) -> str:
    with open(path, "rb") as f:
        b64 = base64.b64encode(f.read()).decode("utf-8")
    
    # MIME-Type ableiten
    lower_path = path.lower()
    if lower_path.endswith(".png"):
        mime = "image/png"
    elif lower_path.endswith((".jpg", ".jpeg")):
        mime = "image/jpeg"
    else:
        mime = "image/png"  # Fallback
    
    return f"data:{mime};base64,{b64}"


client = Client(api_key=os.getenv("GROK_API_KEY"))

#chat = client.chat.create(model="grok-4-1-fast-reasoning")
#chat.append(system("You are Grok, a highly intelligent, helpful AI assistant."))
#chat.append(user("What is the meaning of life, the universe, and everything?"))

#response = chat.sample()
#print(response.content)


chat = client.chat.create(model="grok-4")  
image_path = "../images/splash.png"
data_uri = local_image_to_data_uri(image_path)
chat.append(
    user(
        "Analysiere dieses Bild genau und detailliert.",
        image(data_uri)
    )
)

#chat.append(
#    user(
#        "What's in this image?",                          
#        image("https://science.nasa.gov/wp-content/uploads/2023/09/web-first-images-release.png")
#        
#    )
#)

response = chat.sample()
print(response.content)