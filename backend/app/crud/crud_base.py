from typing import Any, Dict, Generic, List, Optional, Type, TypeVar, Union
from pydantic import BaseModel
from sqlalchemy.orm import Session
from app.database import Base

ModelType = TypeVar("ModelType", bound=Base)
CreateSchemaType = TypeVar("CreateSchemaType", bound=BaseModel)
UpdateSchemaType = TypeVar("UpdateSchemaType", bound=BaseModel)


class CRUDBase(Generic[ModelType, CreateSchemaType, UpdateSchemaType]):
    """
    CRUD 基礎類別
    封裝通用的增刪改查操作
    """

    def __init__(self, model: Type[ModelType]):
        self.model = model

    def get(self, db: Session, id: Any) -> Optional[ModelType]:
        """根據 ID 獲取單一記錄"""
        if db is None:
            return None
        return db.query(self.model).filter(self.model.id == id).first()

    def get_multi(
        self, db: Session, *, skip: int = 0, limit: int = 100
    ) -> List[ModelType]:
        """獲取多筆記錄（優化查詢）"""
        if db is None:
            return []
        return db.query(self.model).offset(skip).limit(limit).all()

    def get_count(self, db: Session) -> int:
        """獲取總記錄數（優化查詢）"""
        if db is None:
            return 0
        return db.query(self.model).count()

    def create(self, db: Session, *, obj_in: CreateSchemaType) -> Optional[ModelType]:
        """建立記錄（優化：添加錯誤處理）"""
        if db is None:
            return None
        try:
            obj_in_data = obj_in.model_dump()
            db_obj = self.model(**obj_in_data)
            db.add(db_obj)
            db.commit()
            db.refresh(db_obj)
            return db_obj
        except Exception as e:
            db.rollback()
            raise e

    def update(
        self,
        db: Session,
        *,
        db_obj: ModelType,
        obj_in: Union[UpdateSchemaType, Dict[str, Any]],
    ) -> Optional[ModelType]:
        """更新記錄（優化：添加錯誤處理）"""
        if db is None:
            return None
        try:
            if isinstance(obj_in, dict):
                update_data = obj_in
            else:
                update_data = obj_in.model_dump(exclude_unset=True)

            for field in update_data:
                if hasattr(db_obj, field):
                    setattr(db_obj, field, update_data[field])

            db.add(db_obj)
            db.commit()
            db.refresh(db_obj)
            return db_obj
        except Exception as e:
            db.rollback()
            raise e

    def delete(self, db: Session, *, id: int) -> Optional[ModelType]:
        """刪除記錄（優化：添加錯誤處理）"""
        if db is None:
            return None
        try:
            obj = db.query(self.model).filter(self.model.id == id).first()
            if obj:
                db.delete(obj)
                db.commit()
            return obj
        except Exception as e:
            db.rollback()
            raise e
