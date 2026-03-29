import os
from PIL import Image

def convert_dir(src_dir, dest_dir):
    if not os.path.exists(dest_dir):
        os.makedirs(dest_dir)
    
    print(f"Converting {src_dir} -> {dest_dir}")
    for filename in os.listdir(src_dir):
        if filename.lower().endswith(('.jpg', '.jpeg', '.png', '.webp')):
            src_path = os.path.join(src_dir, filename)
            # Remove extension and append .png
            base_name = os.path.splitext(filename)[0]
            dest_path = os.path.join(dest_dir, base_name + ".png")
            
            try:
                with Image.open(src_path) as img:
                    img.convert("RGBA").save(dest_path, "PNG")
                print(f"  OK: {filename}")
            except Exception as e:
                print(f"  FAILED: {filename} - {e}")

if __name__ == "__main__":
    convert_dir("card", "card_new")
    convert_dir("bg", "bg_new")
