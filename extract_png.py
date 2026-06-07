import xml.etree.ElementTree as ET
import base64

tree = ET.parse('Assets/Images/logo.svg')
root = tree.getroot()
for child in root.iter():
    if child.tag.endswith('image'):
        href = child.attrib.get('{http://www.w3.org/1999/xlink}href')
        if href and href.startswith('data:image/png;base64,'):
            b64_data = href.split(',')[1]
            with open('Assets/Images/logo.png', 'wb') as f:
                f.write(base64.b64decode(b64_data))
            print("Successfully extracted logo.png")
            break
