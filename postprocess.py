from dataclasses import dataclass
from pathlib import Path
from PIL import Image
import sys

BASE_PATH = "D:\\UMM\\atsdata\\0.49\\sharedassets0.assets\\ExportedProject\\Assets\\Texture2D"
OUT_PATH = "D:\\UMM\\projects\\data-wiki\\img"
SPRITES_FILE = "C:\\Against the Storm\\data-wiki-raw\\sprites_used.txt"
REPLACE = False

@dataclass
class SpriteReference:
    filename: str
    canvas_offset_x: int
    canvas_offset_y: int
    original_width: int
    original_height: int
    target_width: int
    target_height: int

    @property
    def path_in(self) -> Path:
        return Path(BASE_PATH, f"{self.filename}.png")

    def filename_out(self) -> Path:
        filename = f"{self.filename}-{self.target_width}x{self.target_height}"
        if self.canvas_offset_x > 0 or self.canvas_offset_y > 0:
            filename += f"-{self.canvas_offset_x}x{self.canvas_offset_y}"
        return Path(OUT_PATH, f"{filename}.png")
    
    @classmethod
    def from_text_line(cls, text_line):
        values = text_line.strip().split(';')
        return cls(
            filename=values[1],
            canvas_offset_x=int(values[2]),
            canvas_offset_y=int(values[3]),
            original_width=int(values[4]),
            original_height=int(values[5]),
            target_width=int(values[6]),
            target_height=int(values[7])
        )

    def process_image(self):
        file_out = self.filename_out()
        if not REPLACE and file_out.exists():
                return
        with Image.open(self.path_in) as image:
            # Crop the image based on canvas offset and original size
            cropped_image = image.crop(
                (self.canvas_offset_x, image.height-self.canvas_offset_y-self.original_height, self.canvas_offset_x+self.original_width, image.height-self.canvas_offset_y)
            )
            
            # Resize the cropped image to match target size
            resized_image = cropped_image.resize((self.target_width, self.target_height))
            
            # Save the resized image with the new filename
            resized_image.save(file_out)


if __name__ == '__main__':
    
    # Open the text file containing sprite references
    with open(SPRITES_FILE, 'r') as f:
        # Read all lines from the file into a list
        lines = f.readlines()
    
    # Check for duplicates in the list of lines
    seen_lines = set()
    for line in lines:
        if line in seen_lines:
            print(f"Duplicate line found: {line}")
        else:
            seen_lines.add(line)
            # Create a SpriteReference object from the line
            sprite_ref = SpriteReference.from_text_line(line)
            
            # Process the image for the SpriteReference
            sprite_ref.process_image()
