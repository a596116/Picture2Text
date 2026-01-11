from datetime import datetime
from typing import Any, Dict, List, Optional
import json


def datetime_to_str(dt: datetime, fmt: str = "%Y-%m-%d %H:%M:%S") -> str:
    """
    將 datetime 轉換為字串

    Args:
        dt: datetime 物件
        fmt: 格式化字串

    Returns:
        格式化後的字串
    """
    if dt is None:
        return ""
    return dt.strftime(fmt)


def str_to_datetime(dt_str: str, fmt: str = "%Y-%m-%d %H:%M:%S") -> Optional[datetime]:
    """
    將字串轉換為 datetime

    Args:
        dt_str: 日期時間字串
        fmt: 格式化字串

    Returns:
        datetime 物件
    """
    try:
        return datetime.strptime(dt_str, fmt)
    except (ValueError, TypeError):
        return None


def dict_to_json(data: Dict[str, Any], ensure_ascii: bool = False) -> str:
    """
    將字典轉換為 JSON 字串

    Args:
        data: 字典資料
        ensure_ascii: 是否確保 ASCII 編碼

    Returns:
        JSON 字串
    """
    return json.dumps(data, ensure_ascii=ensure_ascii, default=str)


def json_to_dict(json_str: str) -> Dict[str, Any]:
    """
    將 JSON 字串轉換為字典

    Args:
        json_str: JSON 字串

    Returns:
        字典資料
    """
    try:
        return json.loads(json_str)
    except json.JSONDecodeError:
        return {}


def generate_order_no(prefix: str = "ORDER") -> str:
    """
    生成訂單號

    Args:
        prefix: 前綴

    Returns:
        訂單號
    """
    import uuid

    timestamp = datetime.now().strftime("%Y%m%d%H%M%S")
    unique_id = str(uuid.uuid4().hex)[:8].upper()
    return f"{prefix}{timestamp}{unique_id}"


def paginate(items: List[Any], page: int, page_size: int) -> Dict[str, Any]:
    """
    對清單進行分頁

    Args:
        items: 資料清單
        page: 目前頁碼
        page_size: 每頁數量

    Returns:
        分頁結果
    """
    total = len(items)
    start = (page - 1) * page_size
    end = start + page_size
    return {
        "items": items[start:end],
        "total": total,
        "page": page,
        "page_size": page_size,
        "total_pages": (total + page_size - 1) // page_size,
    }
