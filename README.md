# Emo: Backend .NET + Frontend React

Một skeleton dự án full‑stack: ASP.NET Core Web API (backend) và React + Vite (frontend).

## Cấu trúc thư mục

- `server/`: ASP.NET Core 9 Web API (minimal API, OpenAPI)
- `client/`: React + Vite (chưa cài node_modules)
- `EmoApp.sln`: Solution .NET chứa project backend

## Chạy dự án

### Backend (.NET)

```bash
dotnet run --project server
```

hello
Mặc định chạy tại:

- HTTP: `http://localhost:5193`
- HTTPS: `https://localhost:7064`

Các endpoint mẫu:

- `GET /` trả về `{ status: "ok" }`
- `GET /weatherforecast` trả về danh sách dữ liệu mẫu
- `GET /api/todos` danh sách todos (MySQL)
- `POST /api/todos` tạo todo `{ title, isDone }`
- `PUT /api/todos/{id}` cập nhật todo
- `DELETE /api/todos/{id}` xóa todo

### Frontend (React + Vite)

1. Cài dependencies:

```bash
cd client
npm install
```

2. Chạy dev server:

```bash
npm run dev
```

Frontend chạy tại `http://localhost:5173` và đã bật CORS từ backend cho các origin dev phổ biến (`5173`, `3000`).

Nếu backend chạy port khác, chỉnh biến môi trường trong `client/.env.local`:

```
VITE_API_URL=http://localhost:5193
```

### Tính năng Frontend (shop)

- Router: Trang chủ (danh sách sản phẩm, tìm kiếm, lọc theo danh mục), chi tiết sản phẩm, giỏ hàng, thanh toán, đăng nhập/đăng ký, đơn hàng.
- Auth: Lưu JWT trong `localStorage`, tự động thêm header khi gọi API protected.
- Cart: Xem/cập nhật/xóa item; đặt hàng với thông tin giao hàng cơ bản.

Các file chính:

- `client/src/api.js:1` — API client (auth/catalog/cart/orders)
- `client/src/App.jsx:1` — Router + các trang (Home, ProductDetail, Cart, Checkout, Login, Register, Orders)

Lưu ý: cần cài thêm `react-router-dom` (đã khai báo trong `package.json`).

## Ghi chú

- Backend đã bật OpenAPI trong môi trường Development.
- CORS policy tên `FrontendDev` cho phép mọi phương thức/headers từ các origin dev.
- Bạn có thể đổi tên solution/project theo nhu cầu.

## Kết nối MySQL

- Cấu hình chuỗi kết nối trong: `server/appsettings.Development.json:9` và `server/appsettings.json:9` với key `ConnectionStrings:Default`.
- Ví dụ:

```
Server=localhost;Port=3306;Database=emo_db;User Id=root;Password=your_password;SslMode=None;
```

- Bảng `todos` sẽ được tạo tự động khi backend khởi động nếu kết nối hợp lệ.
- Kiểm tra nhanh:

```bash
curl http://localhost:5193/api/todos
curl -X POST http://localhost:5193/api/todos \
  -H "Content-Type: application/json" \
  -d '{"title":"First task","isDone":false}'
```
