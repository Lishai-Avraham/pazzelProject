from flask import Flask, request, jsonify
from PIL import Image, ImageDraw
import io
import base64
import random

app = Flask(__name__)

# --- פונקציית העזר ליצירת המסכה (לא השתנתה) ---
def create_puzzle_piece_mask(size, left, top, right, bottom, tab_radius):
    padding = int(tab_radius * 3)
    width, height = size
    mask_w = width + (padding * 2)
    mask_h = height + (padding * 2)
    
    mask = Image.new('L', (mask_w, mask_h), 0) 
    draw = ImageDraw.Draw(mask)

    x1, y1 = padding, padding
    x2, y2 = x1 + width, y1 + height
    draw.rectangle([x1, y1, x2, y2], fill=255)

    cx_left, cy_left = x1, y1 + (height // 2)
    cx_right, cy_right = x2, y1 + (height // 2)
    cx_top, cy_top = x1 + (width // 2), y1
    cx_bottom, cy_bottom = x1 + (width // 2), y2

    def draw_tab(cx, cy, direction):
        r = tab_radius
        if direction == 1: 
            draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=255)
        elif direction == -1: 
            draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=0)

    draw_tab(cx_left, cy_left, left)
    draw_tab(cx_right, cy_right, right)
    draw_tab(cx_top, cy_top, top)
    draw_tab(cx_bottom, cy_bottom, bottom)

    return mask, padding, mask_w, mask_h

@app.route('/cut_puzzle', methods=['POST'])
def cut_puzzle():
    try:
        # חזרנו לשיטה הישנה והטובה: קבלת קובץ ולא טקסט
        if 'image' not in request.files:
            return jsonify({"status": "error", "message": "No image file provided"}), 400
            
        file = request.files['image']
        rows = int(request.form.get('rows', 2))
        cols = int(request.form.get('cols', 2))

        original = Image.open(file.stream).convert("RGBA")
        img_w, img_h = original.size
        
        piece_w = img_w // cols
        piece_h = img_h // rows
        tab_radius = min(piece_w, piece_h) // 4 

        h_edges = [[0 for _ in range(cols+1)] for _ in range(rows)]
        v_edges = [[0 for _ in range(cols)] for _ in range(rows+1)]

        for r in range(rows):
            for c in range(cols):
                if c < cols - 1: h_edges[r][c+1] = random.choice([1, -1])
                if r < rows - 1: v_edges[r+1][c] = random.choice([1, -1])

        pieces_data = []

        for r in range(rows):
            for c in range(cols):
                edge_left = -h_edges[r][c]
                edge_right = h_edges[r][c+1]
                edge_top = -v_edges[r][c]
                edge_bottom = v_edges[r+1][c]

                mask, padding, final_w, final_h = create_puzzle_piece_mask(
                    (piece_w, piece_h), 
                    edge_left, edge_top, edge_right, edge_bottom, 
                    tab_radius
                )

                # חישוב יחס הגדלה (זה התיקון החשוב)
                scale_x = final_w / piece_w
                scale_y = final_h / piece_h

                crop_x = (c * piece_w) - padding
                crop_y = (r * piece_h) - padding
                
                piece_img = Image.new('RGBA', (mask.width, mask.height), (0,0,0,0))
                
                src_x = max(0, crop_x)
                src_y = max(0, crop_y)
                src_w = min(img_w, crop_x + mask.width) - src_x
                src_h = min(img_h, crop_y + mask.height) - src_y
                
                if src_w > 0 and src_h > 0:
                    snippet = original.crop((src_x, src_y, src_x + src_w, src_y + src_h))
                    paste_x = max(0, -crop_x)
                    paste_y = max(0, -crop_y)
                    piece_img.paste(snippet, (paste_x, paste_y))
                
                piece_img.putalpha(mask)

                buffer = io.BytesIO()
                piece_img.save(buffer, format="PNG")
                img_str = base64.b64encode(buffer.getvalue()).decode('utf-8')

                pieces_data.append({
                    "row": r, "col": c,
                    "image": img_str,
                    "width": piece_img.width, "height": piece_img.height,
                    "scale_x": scale_x, 
                    "scale_y": scale_y
                })

        # מחזירים רק את החלקים במבנה פשוט שיוניטי אוהב
        return jsonify({"pieces": pieces_data})

    except Exception as e:
        print(f"Server Error: {str(e)}")
        return jsonify({"status": "error", "message": str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)