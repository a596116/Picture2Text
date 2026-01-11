# FastAPI + SQLAlchemy + MSSQL 后端应用

基于 FastAPI、SQLAlchemy 和 Microsoft SQL Server 构建的现代化后端应用框架。

## 特性

- **FastAPI**: 高性能的现代化Web框架
- **SQLAlchemy**: 强大的ORM框架
- **MSSQL**: Microsoft SQL Server 数据库支持
- **Swagger UI**: 自动生成的API文档
- **JWT认证**: 安全的用户认证机制
- **CORS支持**: 跨域资源共享
- **日志中间件**: 请求日志记录
- **异常处理**: 统一的异常处理机制
- **分页查询**: 内置分页支持

## 项目结构

```
.
├── app/
│   ├── __init__.py
│   ├── main.py                 # 应用入口
│   ├── config.py               # 配置文件
│   ├── database.py             # 数据库连接
│   ├── api/                    # API路由
│   │   ├── __init__.py
│   │   ├── deps.py             # 依赖注入
│   │   ├── router.py           # 路由汇总
│   │   └── endpoints/          # API端点
│   │       ├── __init__.py
│   │       └── users.py
│   ├── models/                 # 数据库模型
│   │   ├── __init__.py
│   │   └── user.py
│   ├── schemas/                # Pydantic模型
│   │   ├── __init__.py
│   │   └── user.py
│   ├── crud/                   # CRUD操作
│   │   ├── __init__.py
│   │   ├── crud_base.py
│   │   └── crud_user.py
│   ├── core/                   # 核心功能
│   │   ├── __init__.py
│   │   ├── security.py         # 安全相关
│   │   ├── response.py         # 响应模型
│   │   └── exceptions.py       # 自定义异常
│   ├── middleware/             # 中间件
│   │   ├── __init__.py
│   │   ├── logging.py          # 日志中间件
│   │   └── cors.py             # CORS中间件
│   └── utils/                  # 工具函数
│       ├── __init__.py
│       └── common.py
├── .env.example                # 环境变量示例
├── requirements.txt            # 依赖包
└── README.md                   # 说明文档
```

## 快速开始

### 1. 环境要求

- Python 3.8+
- Microsoft SQL Server
- ODBC Driver 17 for SQL Server

### 2. 安装依赖

```bash
python3 -m pip install -r requirements.txt
```

### 3. 配置环境变量

复制 `.env.example` 为 `.env` 并修改配置:

```bash
cp .env.example .env
```

编辑 `.env` 文件，配置数据库连接信息:

```
DB_SERVER=localhost
DB_PORT=1433
DB_NAME=your_database_name
DB_USER=your_username
DB_PASSWORD=your_password
```

### 4. 初始化数据库

```python
from app.database import init_db
init_db()
```

### 5. 运行应用

```bash
# 开发模式（自动重载）
python3 app/main.py

# 或使用 uvicorn
python3 -m uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

### 6. 访问文档

应用启动后，访问以下地址：

- Swagger UI: http://localhost:8000/api/docs
- ReDoc: http://localhost:8000/api/redoc
- OpenAPI JSON: http://localhost:8000/api/openapi.json

## API 示例

### 用户管理

- `GET /api/users/` - 获取用户列表（分页）
- `GET /api/users/{user_id}` - 获取用户详情
- `POST /api/users/` - 创建用户
- `PUT /api/users/{user_id}` - 更新用户
- `DELETE /api/users/{user_id}` - 删除用户

### 创建用户示例

```bash
curl -X POST "http://localhost:8000/api/users/" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "password123"
  }'
```

## 核心功能说明

### 1. CRUD 基类

`app/crud/crud_base.py` 提供了通用的增删改查操作，可以快速创建新的 CRUD 类。

### 2. 统一响应格式

使用 `app/core/response.py` 中的函数返回统一格式的响应：

```python
from app.core.response import success, error, paginated_response

# 成功响应
return success(data=user, message="操作成功")

# 错误响应
return error(message="操作失败", code=400)

# 分页响应
return paginated_response(data=users, total=100, page=1, page_size=10)
```

### 3. 自定义异常

使用 `app/core/exceptions.py` 中定义的异常类：

```python
from app.core.exceptions import NotFoundException, ConflictException

# 抛出异常
raise NotFoundException(detail="用户不存在")
raise ConflictException(detail="用户名已存在")
```

### 4. 工具函数

`app/utils/common.py` 提供了常用的工具函数：

- `datetime_to_str()` - 时间转字符串
- `str_to_datetime()` - 字符串转时间
- `generate_order_no()` - 生成订单号
- `paginate()` - 列表分页

### 5. 密码加密

使用 `app/core/security.py` 中的函数处理密码：

```python
from app.core.security import get_password_hash, verify_password

# 加密密码
hashed = get_password_hash("password123")

# 验证密码
is_valid = verify_password("password123", hashed)
```

## 开发建议

### 添加新的API模块

1. 在 `app/models/` 中创建数据库模型
2. 在 `app/schemas/` 中创建 Pydantic 模型
3. 在 `app/crud/` 中创建 CRUD 类
4. 在 `app/api/endpoints/` 中创建路由
5. 在 `app/api/router.py` 中注册路由

### 数据库迁移

建议使用 Alembic 进行数据库迁移管理：

```bash
pip install alembic
alembic init alembic
```

## 注意事项

1. 生产环境务必修改 `SECRET_KEY`
2. 建议使用数据库迁移工具管理数据库变更
3. 配置合适的 CORS 允许源
4. 启用 HTTPS
5. 实施适当的认证和授权机制

## 许可证

MIT License
