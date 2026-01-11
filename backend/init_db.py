#!/usr/bin/env python3
"""
数据库初始化脚本
运行此脚本以创建所有数据库表
"""
from app.database import init_db


def main():
    """初始化数据库"""
    print("开始初始化数据库...")
    try:
        init_db()
        print("数据库初始化成功！")
        print("所有表已创建。")
    except Exception as e:
        print(f"数据库初始化失败: {str(e)}")
        print("请检查数据库连接配置是否正确。")


if __name__ == "__main__":
    main()
