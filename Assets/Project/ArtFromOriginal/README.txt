ART CỦA BẢN GỐC — PHẢI THAY TRƯỚC KHI PHÁT HÀNH
================================================

Toàn bộ ảnh và font trong thư mục này được trích ra từ bản build gốc bằng AssetRipper.
Chúng CHỈ dùng để dựng lại đúng bố cục / tỉ lệ / cảm giác UI của bản gốc trong lúc phát
triển. Đây KHÔNG phải tài sản của dự án này.

TRƯỚC KHI PHÁT HÀNH BẮT BUỘC PHẢI:
  1. Vẽ lại (reskin) toàn bộ art thay cho các file ở đây.
  2. Thả art mới vào  Assets/Project/Textures/UI/  với ĐÚNG TÊN FILE như ở đây.
     SpriteBinder ưu tiên Textures/UI/ nên art mới sẽ tự động đè lên art gốc.
  3. Chạy menu  Pickleball/UI/Bind Sprites From Folder  để gán lại vào prefab.
  4. Xoá hẳn thư mục Assets/Project/ArtFromOriginal/ khỏi project.

GHI CHÚ KỸ THUẬT
----------------
- Import settings (9-slice border, pivot, pixels-per-unit) được đặt tự động theo
  ui_layout/sprite_metadata.json qua menu  Pickleball/Art/Import Original UI Art.
- Các file PNG ở đây đã bị cắt sát vùng alpha nên nhỏ hơn sprite rect của bản gốc;
  border 9-slice vì vậy được co theo tỉ lệ. Khi vẽ art mới, hãy tự đặt lại border
  trong Sprite Editor cho khớp thiết kế mới.
- Ảnh dùng NÉN CHUẨN, tắt mipmap, tắt crunch; Android override sang ASTC 6x6.
  (Uncompressed cho chất lượng khớp tuyệt đối nhưng ~29 MB PNG sẽ nở thành ~196 MB
  RGBA32 trong build — không dùng được cho mobile.)
- Muốn đối chiếu pixel-perfect với bản gốc thì đổi tạm sang Uncompressed rồi đổi lại.
- Nên gộp vào Sprite Atlas trước khi phát hành để giảm draw call.
